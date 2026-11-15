# Del A – Felsökningsövning

## Problemet
En produkt rapporterades som komplett för kanalen `ecommerce`, trots att den saknade värde för ett obligatoriskt attribut. Felet simulerades i Sortiment.Core. Ett schema (`FamilySchema`) för familjen `t_shirts` skapades i början av arbetet. Sedan lade en admin till attributet `care_instructions` i familjen och gjorde det obligatoriskt för `ecommerce`. När kompletthet beräknades med samma schema var listan över saknade attribut tom.

## Hur jag beskrev det för agenten
Jag gav agenten:
- vad som hände, steg för steg
- förväntat och faktiskt resultat
- ett test som reproducerar felet (`StaleSchemaReproTests`) och dess felutskrift (`should contain "care_instructions" but was actually []`)

Under "vad hade ändrats" angav jag att kompletthetsberäkningen nyligen flyttats till en egen klass (#29).

## Agentens hypoteser
1. **Inaktuellt schema:** schemat kopierar attributdefinitionerna när det skapas, så ett attribut som läggs till senare filtreras bort tyst.
2. **Regression från PR #29:** omstruktureringen ändrade var de obligatoriska attributen hämtas, och nya filtret tappar dem.
3. **Fel scope eller kanal:** ett annat värde räknas som ifyllt, eller kravet hamnade på ett annat kanal-ID.

Varje hypotes kom med konkreta verifieringssteg. Det viktigaste var att jämföra hur många obligatoriska attribut familjen respektive schemat rapporterar.

## Vald hypotes och varför
Jag valde hypotes 1. Symptomet var att kravet *saknades i listan*, inte att ett värde räknades felaktigt som ifyllt. Det pekar på att kravet försvinner innan själva beräkningen. Hypotes 1 gick dessutom att verifiera snabbt.

## Vad som faktiskt var fel
Hypotes 1 stämde:
- Familjen hade 4 obligatoriska attribut, men schemat gav bara 3.
- Med ett nytt schema rapporterades `care_instructions` som saknat.

`FamilySchema` läser kraven från den levande familjen, men filtrerar dem mot de definitioner som kopierades när schemat skapades. Det nya attributet sorteras därför bort utan något fel.

Felet ligger alltså inte i beräkningen. Det ligger i den anropande koden, som återanvänder ett schema efter att familjen ändrats. Det är dokumenterat beteende, men problemet är att Core inte säger ifrån.

## Vad agenten fick rätt och vad den missade
**Rätt:**
- Rätt rotorsak som första hypotes.
- Bra verifieringssteg: antalet obligatoriska attribut skilde direkt ut hypotes 1 från de andra.

**Vilseledande:**
- Hypotes 2 byggde på min egen beskrivning. Jag angav #29 som "vad som ändrats", men felet fanns redan före #29: testet misslyckas även på commiten innan.
- Lärdom: "vad hade ändrats" ska vara det som ändrades innan felet uppstod, inte bara det som ändrades senast.

**Missat:**
- En relaterad bugg: med det gamla schemat ger `SetValue` för `care_instructions` felet `product.attribute_not_in_family`, trots att attributet finns i familjen. Den som försöker fylla i värdet får alltså ett missvisande felmeddelande.
- En detalj som först kom fram i steg 2: `FamilySchema.Create` slänger definitioner för attribut som ännu inte finns i familjen. Det hjälper alltså inte att skicka med extra definitioner i förväg.