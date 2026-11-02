# Säkerhetsgranskning av kodbasen

Inga hårdkodade hemligheter, inloggningsuppgifter eller personuppgifter hittades någonstans i repot eller i dess git-historik. Core har ingen loggning, ingen SQL, ingen HTTP och ingen fil-I/O, så de klassiska riskerna för injection och läckor via loggar finns inte ännu.

Det som hittades är indata som godtas eller ekas tillbaka utan kontroll. Det blir utnyttjningsbart när Storage, Api och Admin (steg 3–4) placerar Cores värden i filsökvägar, HTTP-headers, HTML och loggar.

## Hemligheter och känslig data
- **Hårdkodade hemligheter:** inga. Alla spårade filer och hela git-historiken söktes igenom efter lösenord, tokens, API-nycklar, anslutningssträngar och privata nycklar. Den enda träffen är en kommentar i Visual Studios standardmall, `.gitignore:194`.
- **Personuppgifter:** inga. Inga e-postadresser eller namn förekommer i koden. `LICENSE` och `Directory.Build.props` använder "Sortiment contributors".
- **Loggning:** Core loggar inte. Felmeddelanden innehåller aldrig textvärden från attribut, bara längder och antal (`ValueErrors.cs`), så produktinnehåll läcker inte via fel.

## Saknad validering av indata

### 1. Lagringsnycklar tillåter path traversal (hög risk när en filsystemsadapter finns)
- `src/Sortiment.Core/Files/StorageKey.cs:21-35` kontrollerar bara att nyckeln inte är tom, att den är högst 1 024 tecken och att den saknar kontrolltecken. `../../etc/passwd`, absoluta sökvägar (`/etc/x`, `C:\x`) och `\` godkänns.
- Filsystemsadaptern i steg 3 måste sätta ihop nycklar med en rotkatalog. En nyckel som kommer från lagrad data eller en API-parameter kan då ta sig ut ur roten.

### 2. Textvärden tillåter andra kontrolltecken än radbrytningar (medel)
- `src/Sortiment.Core/Attributes/TextSettings.cs:51` blockerar bara `\r` och `\n`. NUL (`\0`), ESC, backsteg och andra kontrolltecken tillåts i alla textvärden.
- PostgreSQL `text` och `jsonb` avvisar `\0`, så ett sådant värde går igenom valideringen men misslyckas när det sparas.
- ESC-sekvenser kan också manipulera terminalutskrifter när värden loggas eller visas i en konsol.

### 3. Osynliga och riktningsstyrande Unicode-tecken tillåts (medel)
Kontrollerna nedan använder `char.IsControl`, som inte täcker formattecken (kategori Cf) som U+202E (right-to-left override), U+200B (zero-width space) och U+FEFF:
- `src/Sortiment.Core/Products/Sku.cs:46`: SKU:er. `TS-001` och `TS-001` följt av ett nollbreddsmellanslag ser likadana ut men är olika SKU:er. Det möjliggör förfalskning och dubbletter i importer och integrationer.
- `src/Sortiment.Core/Assets/AssetFile.cs:108-110`: filnamn. Ett namn som `manual‮fdp.exe` visas som `manualexe.pdf` (det klassiska RTLO-tricket) vid nedladdning eller i admin.
- `src/Sortiment.Core/Attributes/TextSettings.cs:51`: textvärden går ofiltrerade vidare till flöden och HTML.

### 4. SKU-jämförelsen är inte Unicode-normaliserad (låg)
- `src/Sortiment.Core/Products/Sku.cs:73,76` jämför med `OrdinalIgnoreCase` utan normalisering. Ett förkomponerat `é` (U+00E9) och ett `e` följt av en kombinerande accent blir två olika SKU:er som ser identiska ut.

### 5. Filnamn saneras bara delvis (medel när filer serveras)
- `src/Sortiment.Core/Assets/AssetFile.cs:108-110` avvisar `/`, `\` och kontrolltecken. Följande tillåts:
  - `..` och `.`
  - namn som slutar med punkt eller mellanslag
  - reserverade Windows-namn (`CON`, `NUL`, `COM1`)
  - `"` och `;`
- Om namnet någon gång används i en sökväg eller i en `Content-Disposition`-header kan de här tecknen bryta sig ut ur eller korta av headern. CRLF-injection blockeras, eftersom radbrytningar räknas som kontrolltecken.

### 6. Den angivna medietypen litas på helt (hög risk när API:et serverar filer)
- `src/Sortiment.Core/Assets/AssetFamily.cs:149` validerar den medietyp som anroparen anger, inte filens faktiska innehåll.
- `src/Sortiment.Core/Files/MediaTypePattern.cs:75`: `image/*` matchar även `image/svg+xml`. SVG kan innehålla JavaScript, så en uppladdad SVG som serveras inline från PIM:ens domän är lagrad XSS.
- `src/Sortiment.Core/Files/IFileStorage.cs:23`: `SaveAsync` tar emot den angivna `mediaType` utan någon kontroll av innehållet.

### 7. Filstorlek, kontrollsumma och dimensioner verifieras inte (medel)
- `src/Sortiment.Core/Assets/AssetFile.cs:79,86`: `size` och `checksum` tas emot som de anges. Core kan inte läsa strömmen, så adaptern måste beräkna båda. Inget i API:et gör det tydligt, och om en anropare anger fel storlek kringgås `MaxFileSize` (`AssetFamily.cs:154`).
- `src/Sortiment.Core/Assets/ImageDimensions.cs:14` har ingen övre gräns, så kontroller mot "dekompressionsbomber" lämnas helt åt adaptern.

## Resursutmattning (DoS)

### 8. Inga övre gränser för samlingar och text som standard (medel)
- `src/Sortiment.Core/Attributes/TextSettings.cs:22,63-64`: `MaxLength` är `null` som standard, alltså obegränsad text. `EnumerateRunes().Count()` går dessutom igenom hela strängen.
- `src/Sortiment.Core/Values/MultiSelectValue.cs:21` och `src/Sortiment.Core/Values/AssetCollectionValue.cs:22` kopierar hur många ID:n som helst. Utan `MaxCount` (`AssetCollectionSettings.cs:72`) finns ingen gräns.
- `src/Sortiment.Core/Values/MultiSelectValue.cs:31` skapar en `HashSet` vid varje anrop till `Equals`, så det blir dyrt att jämföra stora värden.

API:et måste begränsa storleken på anrop, eftersom Core inte gör det.

### 9. Felmeddelanden ekar rå indata av valfri längd (låg–medel)
Koder med ogiltig längd läggs in i kontexten i sin helhet:
- `src/Sortiment.Core/CodeRules.cs:25-26,33-34`: en "kod" på 10 MB ekas tillbaka två gånger, en gång i meddelandet och en gång i kontexten.
- Samma sak sker i `src/Sortiment.Core/Products/Sku.cs:43,53,82`, `src/Sortiment.Core/Locale.cs:39-40`, `src/Sortiment.Core/Files/MediaType.cs:48-49`, `src/Sortiment.Core/Files/MediaTypePattern.cs:50-51` och `src/Sortiment.Core/Assets/AssetErrors.cs:13-14`.

Det blåser upp API-svar och loggar. Meddelandena, och de undantag som samlar ihop dem (`src/Sortiment.Core/ResultOfT.cs:46` och `src/Sortiment.Core/CodeRules.cs:43`), kommer också att hamna i loggar. Två sorters tecken når loggarna:
- **Formattecken:** ekas oförändrade, vilket möjliggör förfalskning av loggar, till exempel med U+202E. CR och LF ekas oftast inte, eftersom indata med dem fastnar i valideringen först.
- **Kontrolltecken i koder:** en kod med ett ogiltigt tecken läggs oförändrad in i meddelandet (`CodeRules.cs:33`), radbrytningar inräknade. Det möjliggör log forging om meddelandet loggas som ren text.

## Leveranskedja och CI
- **10. GitHub Actions är låsta till huvudversionstaggar, inte till commit-SHA:er (låg):** `.github/workflows/ci.yml:23,28,46` använder `actions/checkout@v5`, `actions/setup-dotnet@v5` och `actions/upload-artifact@v4`. En komprometterad tagg skulle köras i CI. Behörigheterna är redan minimala (`contents: read`, rad 8–9), vilket begränsar skadan.
- **11. Ingen NuGet-låsfil (låg):** `Directory.Packages.props:9-14` låser exakta versioner, men utan `RestorePackagesWithLockFile` verifieras inte paketens hashvärden vid restore.

## Avsiktligt (ingen åtgärd behövs nu, men måste hanteras i senare steg)
- **Unikhet:** SKU:er och tillgångskoder görs unika först av lagringen (`Products/Sku.cs`, ADR 0010). Utan ett unikt index kan en angripare eller en felaktig import skapa dubbletter.
- **`Restore*` kringgår affärsvalideringen:** `src/Sortiment.Core/Products/Product.cs`, `Restore`. Metoden får aldrig exponeras för indata från API:et (ADR 0011).

## Prioritering
Mest brådskande är punkt 1 (path traversal), punkt 6 (SVG/XSS) och punkt 2 (NUL-tecken), eftersom var och en blir en verklig sårbarhet eller ett verkligt fel så fort steg 3 påbörjas.
