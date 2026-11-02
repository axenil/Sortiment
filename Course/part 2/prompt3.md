# Kodkvalitet: de fem allvarligaste problemen

Problemen är rangordnade efter hur mycket de kommer att kosta i form av buggar eller underhåll.

## 1. Returvärden av typen `Result` ignoreras, så tester kan gå igenom utan att testa något
`Result` har inget stöd från kompilatorn eller någon analysator som tvingar anroparen att kontrollera resultatet, och koden gör det ofta inte. Vissa tester skulle därför kunna gå igenom även om beteendet de kontrollerar vore trasigt:
- **Gemensam fixture:** `tests/Sortiment.Core.Tests/Fixtures/TShirtCatalog.cs:20, 39, 42-45, 48-49` ignorerar resultaten från `SetMaxLength`, `AddAttribute` och `RequireAttribute`. Om något av dem misslyckas körs alla tester som bygger på fixturen mot en annan katalog än de utger sig för, utan någon varning.
- **Round-trip-test:** `tests/Sortiment.Core.Tests/RestoreRoundTripTests.cs` (`ProductsRoundTrip`) kontrollerar inte de fyra anropen till `SetValue`. Om alla misslyckades skulle testet jämföra en tom produkt med en tom produkt och gå igenom, vilket skulle dölja en trasig round-trip.
- **Produkttester:** `tests/Sortiment.Core.Tests/Products/ProductTests.cs:71-72, 118, 143-144, 158-159, 173, 183, 198` sätter upp värden utan att kontrollera att det lyckades.
- **Kompletthetstester:** `tests/Sortiment.Core.Tests/Products/CompletenessTests.cs:128-129` gör samma sak med `RequireAttribute` och `AddLocale`.

Samma problem kommer att dyka upp i applikationstjänsterna från steg 2 och framåt.

## 2. `Product` gör för mycket, och `CalculateCompleteness` är svårläst
- **För många ansvarsområden:** `src/Sortiment.Core/Products/Product.cs` (286 rader) hanterar skapande och återställning (`:78`), värden (`:151`), familjebyten (`:192`), validering (`:217`) och kompletthet (`:250-274`). Varianter kommer att göra klassen ännu större.
- **`CalculateCompleteness` (`:250-271`):** metoden gör tre saker i ett enda nästlat LINQ-uttryck: den slår upp kraven, bygger ett scope för varje locale och filtrerar fram de saknade värdena.
- **`.OfType<AttributeDefinition>()` (`:257`):** används för att filtrera bort `null` från `FindAttribute`. Det är ett trick som inte är självklart, och ingen kommentar förklarar varför ett obligatoriskt attribut skulle kunna sakna definition. Det händer när ett attribut läggs till i familjen efter att schemat skapades.

## 3. "attribute" betyder fyra olika saker
Samma namn används för en `AttributeDefinition`, ett `AttributeId`, en `AttributeCode` och en vanlig `string`, ibland i samma klass:
- **`TextSettings`:** i `src/Sortiment.Core/Attributes/TextSettings.cs` är fältet `_attribute` (`:15`) en `AttributeCode`. I samma klass är parametern `attribute` (`:40, :43`) en `AttributeDefinition`.
- **`SelectSettings`:** `src/Sortiment.Core/Attributes/SelectSettings.cs:12` jämfört med `:37, :56, :61` har samma blandning.
- **Felhjälpare:** `src/Sortiment.Core/Families/FamilyErrors.cs:14` och `src/Sortiment.Core/Products/ProductErrors.cs:7` tar emot `string attribute`, som ibland är en kod och ibland ett GUID beroende på anroparen. Det är grundorsaken till den inkonsekventa felkontexten (H2).
- **Andra otydliga namn:**
  - `CodeRules.ValueOrThrow` (16 användningar) används även för `Locale`, `Sku`, `MediaType` och `Restore`, som inte är koder.
  - De privata `Build`-metoderna i `Channel.cs`, `AssetFamily.cs` och `AttributeDefinition.cs` säger inget om vad de validerar.
  - `Restore` är en statisk fabriksmetod, men `RestoreOption` är en instansmetod som lägger till ett alternativ.

## 4. `Family.Restore` gör flera saker och hanterar fel på ett förvirrande sätt
`src/Sortiment.Core/Families/Family.cs:71-111`:
- **Flera uppgifter i en metod:** metoden bygger familjen, kontrollerar dubblerade attribut och kontrollerar kraven, allt i samma loop.
- **Data städas och avvisas ändå:** rad `:101` filtrerar tyst bort ogiltiga krav (`required.Where(family.HasAttribute)`), men `:106` kastar ett undantag för dem ändå. Läsaren kan inte avgöra om felaktig data ska städas eller avvisas.
- **En tredje felstil:** rad `:87` och `:98` samlar problem som vanliga strängar. På andra ställen går `Channel.Restore`, `AssetFamily.Restore` och `AttributeDefinition.Restore` via `ResultError` och `ValueOrThrow`, och `Product.Restore` kastar vid första problemet. Det blir tre olika felstilar för samma sorts operation.

## 5. Kluriga algoritmer utan förklarande kommentar
De här är svåra att förstå utan en kommentar om varför:
- **Likhet för `MultiSelectValue`:** `src/Sortiment.Core/Values/MultiSelectValue.cs:31, 35` använder `Distinct().Aggregate(0, (hash, option) => hash ^ ...)`. Det framgår inte att XOR finns där för att göra hashvärdet oberoende av ordningen, eller att `[a, a]` och `[a]` avsiktligt räknas som lika.
- **Validering av flerval:** `src/Sortiment.Core/Attributes/SelectSettings.cs:140-150` använder två `HashSet`, `seen` och `reportedDuplicates`, plus en `continue`. Utan kommentar är det svårt att se att varje dubblett rapporteras en gång och att upprepningar inte kontrolleras som okända alternativ.
- **`AssetCode.FromFileName`:** `src/Sortiment.Core/Assets/AssetCode.cs:56-80` normaliserar till FormD och hoppar över `NonSpacingMark` (`:56-58`) för att ta bort accenter (`å` → `a`). Den kortar också koden till maxlängden innan avslutande `_` tas bort (`:77-80`), så att koden aldrig slutar med `_`. Inget av stegen förklaras. `Path.GetFileNameWithoutExtension` (`:56`) beter sig dessutom olika på Windows och Linux för namn som innehåller `\`.
