# Del 2: Analys och implementation

## Vad identifierade agenten?

Agenten gick igenom hela `Sortiment.Core` (cirka 4 000 rader) och delade in fynden efter allvarlighetsgrad.

**Hög**, alltså saker som blockerar kommande steg:
- **H1:** Aggregaten gick inte att återskapa från lagrad data. Alla konstruktorer var privata och `Create` skapade alltid ett nytt ID.
- **H2:** Felkontext var inkonsekvent. Ibland innehöll den koder och ibland GUID:er för attribut och kanaler.
- **H3:** Collection-egenskaper som `Product.Values` hade ostabil ordning och allokerade en ny lista vid varje anrop.
- **H4:** Reglerna för scope (locale/kanal) fanns på flera ställen.
- **H5:** En ny attributtyp kräver ändringar på många ställen, och kompilatorn fångar inte om något saknas.

**Medel**, alltså duplicering och inkonsekvens:
- **ID- och kodtyper:** de är kopierade nästan ordagrant för varje entitet.
- **Upprepade mönster:** samma felhantering och dublettkontroller är skrivna för hand på flera ställen.
- **Strängregler:** reglerna skiljer sig åt mellan typerna, till exempel om längd räknas i tecken eller code points.
- **API-form:** ibland tar metoderna ID och ibland definition, och vissa operationer är idempotenta medan andra returnerar fel.
- **Felkoder:** de är utspridda och saknar publika konstanter.
- **`FamilySchema`:** typen är varken en ögonblicksbild eller en levande vy.
- **`MultiSelectValue`:** likhetsjämförelsen stämmer inte med datan.

**Låg**, alltså läsbarhet:
- interna typnamn i felmeddelanden
- ingen `samples/`-mapp
- duplicerade testhjälpare

## Vad ville agenten göra som du stoppade och varför?

- **Gå vidare utan att prioritera.** Efter analysen ville agenten gå vidare med analysen som plan och sedan direkt fortsätta med vidare implementering av projektet. Jag stoppade det och bad i stället om en konkret plan för enbart H1: vilka filer som ändras, vad som ändras och i vilken ordning. Jag ville granska planen innan något implementerades, eftersom H1 är ett arkitekturbeslut som påverkar alla aggregat och lagringen i steg 3.
- Själva lösningen som föreslogs gjorde jag inga ändringar för

## Vad missade den?

- Jag fångade själv inga saker som missades, men Claude själv flaggar nedan som saker som missades.
- **`Restore` är ett publikt API som kringgår affärsregler.** Vem som helst kan anropa `Product.Restore` med värden för attribut som inte finns i familjen, eller med ogiltiga värden. ADR:en säger att metoden bara ska användas för lagrad data, men inget i koden tvingar fram det.
- **Inkonsekvent felhantering vid återställning.** Inställningar återställs via befintliga setters, som returnerar `Result` i stället för att kasta undantag. Det går emot beslutet att `Restore` ska kasta vid trasig data, och lagringsadaptern måste själv kontrollera varje resultat.
- **Ingen hantering av samtidighet eller metadata.** `Restore` tar inte emot någon version eller concurrency-token och inga tidsstämplar (skapad/ändrad). Det kommer att behövas i lagringssteget.
- **Ingen kontroll av referenser.** `Family.Restore` kontrollerar inte att kanalerna finns, och `Asset.Restore` kontrollerar inte filen mot familjen. Det är ett medvetet val, men det betyder att trasiga referenser bara upptäcks senare.
- **Nya fel enligt H2.** Felmeddelandena i `Family.Restore` och `Product.Restore` använder attribut-ID:n, så samma problem som H2 beskriver uppstår igen i ny kod.
- **Analysen byggde bara på kodläsning.** Den använde inga mätverktyg, som testtäckning eller komplexitetsmått, och saknade luckor fångades därför inte systematiskt.

## Är projektet körbart efter implementationen?

Ja, i den mening som är möjlig i dag. `Sortiment.Core` är ett bibliotek utan körbar applikation eller exempel.

- `dotnet build` går igenom utan varningar.
- Alla 347 tester går igenom, och `dotnet format --verify-no-changes` är rent. Det gäller både lokalt och i CI på GitHub Actions för båda PR:erna (#15 och #16).
- Det finns ännu ingenting att starta och köra manuellt. API, lagring och exempelprojekt kommer i senare steg.
