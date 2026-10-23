# CalendarGenerator

**CalendarGenerator** to aplikacja konsolowa (.NET 10 / C#) służąca do automatycznego pobierania planu zajęć z uczelnianego kalendarza (np. **USOS AGH**) w formacie iCalendar (`.ics`) i generowania estetycznego, gotowego do wydruku lub ustawienia na tapetę planu tygodniowego w formacie **PNG** (proporcje A4 w układzie poziomym).

---

## Jak działa program

1. **Dynamiczne pobieranie planu z sieci**:
   Przy starcie aplikacja łączy się z podanym adresem URL kalendarza `.ics` (np. link subskrypcji iCal z USOS) i pobiera najbardziej aktualny harmonogram zajęć.
2. **Parsowanie i klasyfikacja zajęć**:
   Program przetwarza pobrane dane i rozpoznaje typy zajęć (zarówno po pełnych nazwach, jak i skrótach USOS):
   - **W** – Wykład (`Lecture`)
   - **LAB** / **CWL** – Laboratoria (`Laboratory`)
   - **ĆW** / **CWA** – Ćwiczenia audytoryjne (`Tutorial`)
   - **P** / **CWP** – Ćwiczenia projektowe (`Project`)
   - **L** / **LEKT** – Lektorat (`Language`)
   - **WF** – Wychowanie fizyczne (`PE`)
   - **Inne** – Pozostałe zajęcia (`Other`)
3. **Filtrowanie tygodnia**:
   Użytkownik podaje dowolną datę z docelowego tygodnia (lub zatwierdza bieżący dzień klawiszem Enter). Program automatycznie wyznacza poniedziałek danego tygodnia i filtruje zajęcia od poniedziałku do niedzieli.
4. **Możliwość modyfikacji tygodnia przed wygenerowaniem**:
   - Możesz dodać własne, cykliczne lub jednorazowe zajęcia spoza planu (np. treningi, korepetycje, koła naukowe) – zapisywane są one w pliku `additional_classes.csv`.
   - Możesz dopisać do konkretnych zajęć dodatkowe wydarzenia (*Extra Events*, np. kolokwium, termin oddania sprawozdania) wraz z priorytetem (*Low*, *Medium*, *High*).
5. **Generowanie obrazu graficznego (SkiaSharp)**:
   Program renderuje siatkę tygodnia o wysokiej rozdzielczości (domyślnie 300 DPI dla formatu A4 poziom), automatycznie dopasowując szerokości kolumn (dni robocze mają standardową szerokość, a weekend jest węższy), zawijając tekst i wyróżniając kafelki kolorami.

---

## Ważne: Konfiguracja (config.json)

> [!IMPORTANT]
> **Przed pierwszym uruchomieniem programu należy otworzyć plik `Data/config.json` i ręcznie dostosować ustawienia do własnych potrzeb.**
> W szczególności należy wkleić własny link do kalendarza `.ics` oraz upewnić się, że ścieżki zapisu odpowiadają strukturze katalogów na Twoim komputerze.

Plik konfiguracyjny znajduje się w ścieżce:
`Data/config.json`

### Przykładowy plik `config.json`
```json
{
  "IcsUrl": "https://apps.usos.agh.edu.pl/services/tt/upcoming_ical?lang=pl&user_id=TWÓJ_ID&key=TWÓJ_KLUCZ",
  "SaveFilepath": ".\\Data\\schedule_output.png",
  "AdditionalClassesFilepath": ".\\Data\\additional_classes.csv",
  "PaperSize": 2,
  "Dpi": 300,
  "Font": "Calibri",
  "BackgroundColor": "#FFFFFFFF",
  "HightlightColor": "#FFDDDDDD",
  "LowPriorityEventColor": "#FF00FF00",
  "MediumPriorityEventColor": "#FF0000FF",
  "HighPriorityEventColor": "#FFFF0000",
  "BorderMargin": 5,
  "SmallFontSize": 6.5,
  "StandardFontSize": 8,
  "BigFontSize": 11.0,
  "HightlightLectures": true
}
```

### Opis parametrów konfiguracyjnych

| Parametr | Typ | Opis |
| :--- | :--- | :--- |
| `IcsUrl` | `string` | **Kluczowe ustawienie.** Bezpośredni adres URL (lub lokalna ścieżka do pliku `.ics`) do subskrypcji kalendarza iCal (np. wygenerowany w USOSweb w sekcji *Mój USOSweb -> Plan zajęć -> Eksportuj / iCal*). |
| `SaveFilepath` | `string` | Bezwzględna lub względna ścieżka pliku wynikowego PNG, pod którą zostanie zapisany wygenerowany harmonogram. |
| `AdditionalClassesFilepath` | `string` | Ścieżka do pliku CSV przechowującego ręcznie dopisane zajęcia. Jeśli plik nie istnieje, program utworzy go automatycznie przy pierwszym dodaniu zajęć. |
| `PaperSize` | `int` | Wskaźnik formatu arkusza (domyślnie `2` odpowiada arkuszowi standardu proporcji A4). |
| `Dpi` | `int` | Gęstość pikseli na cal (DPI). Wartość `300` gwarantuje jakość fotograficzną do druku A4 lub ostry podgląd na monitorach o wysokiej gęstości pikseli. Dla mniejszych plików można ustawić np. `150` lub `96`. |
| `Font` | `string` | Nazwa rodziny czcionki zainstalowanej w systemie (np. `"Calibri"`, `"Arial"`, `"Segoe UI"`). |
| `BackgroundColor` | `string` (Hex `#AARRGGBB`) | Kolor tła całego kalendarza. Przykłady: `#FFFFFFFF` (biały kryjący), `#00000000` (przezroczysty), `#FFF5F5F5` (jasnoszary). |
| `HightlightColor` | `string` (Hex `#AARRGGBB`) | Kolor tła kafelków dla wykładów, jeśli opcja `HightlightLectures` jest włączona (np. `#FFDDDDDD` – delikatne szare tło). |
| `LowPriorityEventColor` | `string` (Hex `#AARRGGBB`) | Kolor ramki i tekstu dla dodatkowych wydarzeń o **niskim priorytecie** (Low), np. drobne zadania domowe. |
| `MediumPriorityEventColor`| `string` (Hex `#AARRGGBB`) | Kolor ramki i tekstu dla dodatkowych wydarzeń o **średnim priorytecie** (Medium), np. wejściówki, sprawozdania. |
| `HighPriorityEventColor` | `string` (Hex `#AARRGGBB`) | Kolor ramki i tekstu dla dodatkowych wydarzeń o **wysokim priorytecie** (High), np. kolokwia, egzaminy, obrony projektów. |
| `BorderMargin` | `int` | Zewnętrzny margines arkusza wyrażony w milimetrach (domyślnie `5`). |
| `SmallFontSize` | `float` | Rozmiar czcionki (w punktach typograficznych pt) używany do wyświetlania godzin zajęć, kodów typów (np. `8:00, W`) oraz opisów wydarzeń. |
| `StandardFontSize` | `float` | Rozmiar czcionki (w punktach pt) używany do wyświetlania nazw przedmiotów oraz tytułów wydarzeń. |
| `BigFontSize` | `float` | Rozmiar czcionki (w punktach pt) dla nagłówków kolumn (nazwy dni tygodnia: Poniedziałek – Niedziela). |
| `HightlightLectures` | `bool` | Określa, czy wykłady mają mieć wyróżnione tło kolorem `HightlightColor` (`true`), czy białe jak pozostałe zajęcia (`false`). |

---

## Instrukcja obsługi krok po kroku

### 1. Uruchomienie programu
Pobierz program z aktualnej [dystrybucji](https://github.com/KrzemienPasiasty/CalendarGenerator/releases/latest).

Po poprawnym skonfigurowaniu `config.json` uruchom aplikację otwierając plik `CalendarGenerator.exe`.

Program wyświetli komunikat o pobieraniu planu:
```text
=== Weekly Schedule PNG Generator ===

Downloading schedule (.ics)...
Loaded 180 total class entries.
```

### 2. Wybór tygodnia
W konsoli pojawi się zapytanie o datę:
```text
Enter any date in target week (e.g. DD.MM.YYYY), or press Enter for today:
```
- Wciśnij **Enter**, aby wygenerować plan na bieżący tydzień.
- LUB wpisz dowolną datę należącą do interesującego Cię tygodnia (np. `26.10.2026`) i zatwierdź.
Program wskaże poniedziałek danego tygodnia i poinformuje, ile zajęć znaleziono w tym przedziale.

### 3. Menu główne
Na ekranie pojawi się wybór:
```text
Generate schedule image now: ANY KEY
Add additional classes: 1
Add extra events to classes: 2
```

#### Opcja A: Natychmiastowe wygenerowanie grafiki (Dowolny klawisz)
Wciśnij dowolny klawisz inny niż `1` lub `2`. Program wygeneruje plik PNG w ścieżce wskazanej w `SaveFilepath`:
```text
Successfully generated image: ...\Data\schedule_output.png
```

#### Opcja B: Dodanie własnych zajęć (Klawisz `1`)
Umożliwia dopisanie zajęć spoza planu uczelni:
1. Podaj nazwę zajęć (np. `Trening siłowy`, `Lekcja niemieckiego`).
2. Podaj datę w formacie `DD.MM.YYYY`.
3. Podaj godzinę rozpoczęcia w formacie `HH:mm` (np. `18:00`).
4. Podaj godzinę zakończenia w formacie `HH:mm` (np. `19:30`).
5. Zajęcia zostaną zapisane w pliku `additional_classes.csv` i będą uwzględniane przy generowaniu planu.

#### Opcja C: Dodanie dodatkowych wydarzeń do zajęć (Klawisz `2`)
Program wyświetli po kolei zajęcia z wybranego tygodnia. Dla każdego z nich możesz:
1. Wcisnąć `y`, aby dodać wydarzenie (np. kolokwium, wejściówkę).
2. Wpisać nazwę wydarzenia (np. `Kolokwium 1`).
3. Wpisać krótki opis (np. `Zakres: tematy 1-4`).
4. Wybrać priorytet:
   - `1` – Niski (zielona ramka)
   - `2` – Średni (niebieska ramka)
   - `3` – Wysoki (czerwona ramka)
Wydarzenie zostanie wyrenderowane wewnątrz ramki danych zajęć w wybranym kolorze.

> [!IMPORTANT]
> **Uwaga do wprowadzania wyborów.**
> Wybory, które składają się z pojedynczego znaku (jak np. prorytet, wybór dodania nowego wydarzenie) nie wymagają zatwierdzenia wyboru za pomocą Entera.
> Wybory wieloznakowe wymagają go.
> Stąd też należy uważnie wprowadzać wybór i analizować, czy opcja już się aktywowała, czy nie.

---

## Wymagania i uruchomienie
W przypadku uruchamiania pliku .exe wszystko powinno działać.

Przy próbie kompilacji z kodu źródłowego wymagane będzie:

- **Środowisko**: [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **Zależności NuGet** (przywracane automatycznie):
  - `SkiaSharp` (renderowanie grafiki 2D)
  - `Newtonsoft.Json` (obsługa konfiguracji JSON)
  - `System.Drawing.Common`
- **Czcionki**: Upewnij się, że czcionka podana w polu `"Font"` w `config.json` jest zainstalowana w systemie Windows (w przeciwnym razie SkiaSharp użyje czcionki zastępczej).
