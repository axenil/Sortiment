# Arkitekturanalys mot principerna i CLAUDE.md

Ingen princip bryts tydligt där det verkligen spelar roll. Core har fortfarande inga beroenden, ingen I/O och inget statiskt föränderligt tillstånd. Det finns dock några tydliga avvikelser, de flesta små, och flera ställen där en princip tänjs eller där CLAUDE.md och en ADR säger emot varandra.

## Tydliga avvikelser

### 1. Princip 16 (varje paket har en README och ett körbart exempel i `samples/`)
- `src/Sortiment.Core/README.md` finns, men det finns ingen `samples/`-mapp alls i repot.

### 2. Princip 6 ("interna referenser använder alltid ID:n") säger emot ADR 0007
- `src/Sortiment.Core/Values/MeasurementValue.cs` refererar till sin enhet med `UnitCode`, inte med ett ID.
- `src/Sortiment.Core/Attributes/MeasurementSettings.cs` håller själva `MeasurementFamily`-objektet, inte ett ID.
- `src/Sortiment.Core/Measurements/MeasurementFamily.cs` har en kod men inget ID, trots att princip 6 säger att definitionsentiteter har båda.
- ADR 0007 valde detta medvetet ("Families and units are referenced by code"), men CLAUDE.md uppdaterades aldrig. Dokumenten säger alltså emot varandra, och det får betydelse när användare ska kunna skapa egna måttfamiljer.

### 3. Princip 13 (varje beteendeändring kommer med tester)
- Bara `AttributeId` har egna ID-tester (`ChannelCodeTests.cs` testar även `ChannelId`). Följande typer saknar egna testfiler:
  - ID:n: `FamilyId`, `ProductId`, `AssetId`, `AssetFamilyId`, `AttributeOptionId`
  - Koder: `FamilyCode`, `AssetFamilyCode`, `MeasurementFamilyCode`, `UnitCode`
  - Övriga typer: `ImageDimensions`, `MeasurementFamilies`
- De flesta täcks indirekt, till exempel via `CodeRules`, men deras egna regler, som att `Guid.Empty` avvisas, testas inte typ för typ.

### 4. Namnkonventionen för filer ("en publik typ per fil, med filen namngiven efter typen")
- `src/Sortiment.Core/ResultOfT.cs` innehåller `Result<T>`. `ResultOfT` är ett vanligt mönster i .NET för generiska typer, men det följer inte regeln som den är skriven. Det är den enda fil som avviker.

## Principer som tänjs

### 5. Princip 11 (records för värdeobjekt)
- `src/Sortiment.Core/Products/LocaleCompleteness.cs` och `ChannelCompleteness.cs` är oföränderliga resultatobjekt men är klasser. Två likadana resultat jämförs därför inte som lika.
- `src/Sortiment.Core/Measurements/MeasurementFamily.cs` är oföränderlig och identifieras bara av sin kod, så den beter sig som ett värdeobjekt, men den är en klass med referenslikhet.
- `src/Sortiment.Core/ResultError.cs` är en klass. ADR 0005 förklarar bara varför `Result` är en klass, inte `ResultError`.

### 6. Princip 4 (affärsregler ligger i domänen)
Regler som spänner över flera aggregat lämnas åt anroparen och hamnar därför i applikationstjänsterna:
- `src/Sortiment.Core/Attributes/SelectSettings.cs` (`RemoveOption`): kommentaren säger att anroparen måste se till att ingen produkt fortfarande använder alternativet.
- `src/Sortiment.Core/Families/Family.cs` (`RemoveAttribute`): värden ligger kvar på produkterna tills en tjänst städar bort dem (ADR 0010).
- SKU:er (`Products/Sku.cs`) och tillgångskoder inom en familj (`Assets/Asset.cs`) görs unika av lagringen.
- De publika `Restore*`-metoderna hoppar över affärsvalideringen, och det är bara ADR:en som säger att de enbart ska användas för lagrad data.

Allt detta är dokumenterat och delvis oundvikligt, eftersom ett enskilt aggregat inte kan se de andra. Ändå äger domänen inte de här reglerna.

### 7. Princip 7 (ett ansvar per typ)
- `src/Sortiment.Core/Products/Product.cs` hanterar värden, familjebyten, validering, återställning och nu även kompletthet (`CalculateCompleteness` med `ScopeFor`).
- `src/Sortiment.Core/Attributes/AttributeDefinition.cs` hanterar skapande, återställning, scope-regler, vidarekoppling av valideringen till inställningarna och felkontext.
- Inställningstyperna är beroende av sin förälders `Type`, så de två pekar på varandra:
  - `TextSettings.cs` (`ValidateText`) kontrollerar `TextArea` och `Identifier`.
  - `SelectSettings.cs` (`ValidateValue`) växlar på `SingleSelect` och `MultiSelect`.

### 8. Princip 10 (strukturerade fel med kontext, till exempel attributet)
- Kontexten innehåller ibland en kod och ibland ett GUID. Det är fynd H2 från den tidigare analysen:
  - `Families/FamilyErrors.cs`: `AttributeNotInFamily` anropas med `attribute.ToString()` från `Family.RemoveAttribute`.
  - `Products/Product.cs`: `Validate` skickar `attributeId.ToString()`.
  - `Attributes/AttributeDefinition.cs`: `AddScopeContext` lägger ett kanal-GUID under `ErrorContextKeys.Channel`.
- `src/Sortiment.Core/Files/IFileStorage.cs` blandar stilar: `OpenReadAsync` returnerar `Result<Stream>`, medan `SaveAsync` och `DeleteAsync` bara signalerar fel genom att kasta undantag.

### 9. Namnkonventionen (namnge inte typer efter vanliga BCL-typer)
- `src/Sortiment.Core/Channels/Channel.cs` heter samma sak som `System.Threading.Channels.Channel`. Det namnrummet finns inte bland de implicita using-satserna, så vi påverkas inte i dag, men konsumenter som använder channels för bakgrundsarbete får en namnkonflikt.
- `Asset.File` i `Assets/Asset.cs` och `AssetTestData.File(...)` i testerna skuggar `System.IO.File`, som är en implicit using. Regeln gäller typnamn, så detta är en mindre sak.

## Principer som följs
- **1 (beroenden pekar inåt):** `tests/Sortiment.ArchitectureTests/CoreDependencyTests.cs` verifierar att Core bara refererar till BCL. Api och Admin finns inte ännu, så den andra halvan av regeln har inget att kontrollera.
- **2 (ren kärna):** det finns ingen I/O, ingen loggning och inget utanför BCL. `AssetCode.FromFileName` använder bara `Path` för stränghantering, och `FileChecksum` använder bara en SHA-256-konstant.
- **3 (verktyg riktar sig mot abstraktioner):** `IFileStorage` är definierad i Core. `IProductTarget` och `IProductSource` finns inte ännu; de hör till steg 2.
- **5, 9, 14 och 15:** det finns inga tjänster ännu, alla asynkrona metoder tar en `CancellationToken`, och Core använder varken options eller loggning.
- **8 (inget globalt föränderligt tillstånd):** de enda statiska medlemmarna är oföränderliga: `MeasurementFamilies`, `ValueScope.Global` och `Result.SuccessResult`.
- **12 (det publika API:et är ett kontrakt):** XML-dokumentation krävs av bygget, och felhjälparna är `internal`.

## Beslut som behövs
Det enda som kräver ett beslut är punkt 2. Antingen får princip 6 i CLAUDE.md ett undantag för måttfamiljer, eller så ses ADR 0007 över innan användardefinierade måttfamiljer införs. Resten kan planeras som åtgärder på samma sätt som de tidigare fynden.
