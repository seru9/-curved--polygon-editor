# Q&A - Polygon Editor

## Pytanie: Po co mi OnPropertyChanged?

`OnPropertyChanged` jest niezbędna dla powiązania danych (data binding) w WPF. Kiedy zmienia się właściwość `Position` lub `Continuity`, metoda powiadamia interfejs użytkownika o tej zmianie przez zdarzenie `PropertyChanged`. Bez tego WPF nie wie, że model się zmienił, i UI nie zostanie zaktualizowany. Dzięki temu powiązanie danych działa automatycznie — zmiana w modelu natychmiast pojawia się w interfejsie użytkownika.

---

## Pytanie: Po co mi ObservableCollection zamiast List?

`ObservableCollection<T>` automatycznie powiadamia UI o każdej zmianie w kolekcji: dodaniu, usunięciu czy zmianie kolejności elementów. `List<T>` tego nie robi — dodanie wierzchołka do `_vertices` nie odświeży interfejsu. W WPF `ObservableCollection<T>` jest standardem dla kolekcji w UI (np. `ListBox`, `DataGrid`), ponieważ zapewnia, że zmiany danych natychmiast odzwierciedlają się w oknie. Bez niej musisz ręcznie wywoływać `OnPropertyChanged`, co jest podatne na błędy.

---

## Pytanie: Do czego służy SubtractConverter?

`SubtractConverter` to konwerter wartości (`IValueConverter`), który odejmuje parametr od wartości wejściowej. W kontekście renderowania wielokątów (ellipse o rozmiarze 8×8 pikseli), odejmuje 4 od współrzędnych X i Y, aby wyśrodkować punkt wierzchołka. Zamiast ustawiać lewy górny róg na pozycji wierzchołka, przesuwamy go o połowę rozmiaru (4px), co powoduje, że środek ellipsy pokrywa się z faktyczną pozycją wierzchołka.

---

## Pytanie: Dlaczego Canvas.Left/Top ustawione na Ellipse wewnątrz DataTemplate nie działa?

W WPF `Canvas` pozycjonuje swoje **bezpośrednie dzieci**. Gdy używasz `ItemsControl` z `Canvas` jako panelem (`ItemsPanel`), bezpośrednimi dziećmi `Canvas`a są `ContentPresenter`y — kontenery owijające każdy element. Elementy zdefiniowane w `ItemTemplate` (np. `Ellipse`) są zagnieżdżone *wewnątrz* `ContentPresenter`, przez co attached properties `Canvas.Left`/`Canvas.Top` ustawione na `Ellipse` są **całkowicie ignorowane** przez `Canvas`.

**Rozwiązanie:** Użyj `ItemContainerStyle`, aby ustawić `Canvas.Left`/`Canvas.Top` na `ContentPresenter` — który jest faktycznym bezpośrednim dzieckiem `Canvas`a:

```xml
<ItemsControl.ItemContainerStyle>
    <Style TargetType="ContentPresenter">
        <Setter Property="Canvas.Left" Value="{Binding X}" />
        <Setter Property="Canvas.Top"  Value="{Binding Y}" />
    </Style>
</ItemsControl.ItemContainerStyle>
<ItemsControl.ItemTemplate>
    <DataTemplate>
        <Ellipse Width="8" Height="8" Fill="Red" /> <!-- bez Canvas.Left/Top! -->
    </DataTemplate>
</ItemsControl.ItemTemplate>
```

---

## Pytanie: Do czego służy EdgeToGeometryConverter?

`EdgeToGeometryConverter` to konwerter (`IValueConverter`), który tłumaczy obiekt modelu `Edge` na obiekt `Geometry` zrozumiały przez WPF. Element `<Path>` w XAML nie wie nic o klasie `Edge` — oczekuje obiektu `Geometry` (np. `LineGeometry`, `PathGeometry`). Konwerter pobiera `StartVertex` i `EndVertex` krawędzi i tworzy `LineGeometry`. Dzięki temu logika rysowania jest **oddzielona od modelu** — w przyszłości łatwo obsłużyć `EdgeType.Bezier` czy `EdgeType.Arc` dodając kolejne przypadki w konwerterze, bez zmiany modelu danych.

---

## Pytanie: Po co w XAML używamy ItemsControl z ItemsPanel jako Canvas?

Domyślny panel `ItemsControl` to `StackPanel`, który układa elementy jeden pod drugim. Aby swobodnie pozycjonować elementy na płótnie (wielokąty, wierzchołki, krawędzie), zamieniamy go na `Canvas`:

```xml
<ItemsControl.ItemsPanel>
    <ItemsPanelTemplate>
        <Canvas />
    </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

Dzięki temu każdy element (`ContentPresenter`) może mieć dowolne współrzędne `Canvas.Left`/`Canvas.Top`, a `ItemsControl` zachowuje możliwość bindowania do kolekcji (`ObservableCollection`).

---

## Pytanie: Dlaczego właściwości X i Y w Vertex są tylko do odczytu (get-only)?

```csharp
public double X => _position.X;
public double Y => _position.Y;
```

`X` i `Y` są wygodnymi skrótami do bindowania w XAML — zamiast pisać `{Binding Position.X}`, piszemy `{Binding X}`. Są tylko do odczytu, bo `Point` (struktura) jest niemutowalny — zmiana pozycji wierzchołka odbywa się przez przypisanie całego nowego `Point` do właściwości `Position`, co wyzwala `OnPropertyChanged(nameof(Position))`.

**Uwaga:** `X` i `Y` **nie** wywołują `OnPropertyChanged` — przy zmianie `Position` należy osobno powiadamiać o `X` i `Y`, jeśli UI jest do nich zbindowane:

```csharp
set {
    _position = value;
    OnPropertyChanged(nameof(Position));
    OnPropertyChanged(nameof(X)); // ← wymagane dla bindowania!
    OnPropertyChanged(nameof(Y));
}
```

---

## Pytanie: Po co Polygon implementuje INotifyPropertyChanged, skoro wierzchołki są w ObservableCollection?

`ObservableCollection<Vertex>` powiadamia UI o dodaniu/usunięciu wierzchołków z kolekcji, ale **nie** powiadamia o tym, że sama właściwość `Vertices` zmieniła referencję (np. przypisaniu nowej kolekcji). `OnPropertyChanged` w `Polygon` obsługuje ten drugi przypadek — gdy całe `Vertices` lub `Edges` zostaje zastąpione nową kolekcją. W praktyce jest to rzadkie, ale potrzebne dla pełnej poprawności bindowania.

---

## Pytanie: Po co w MainWindow.xaml jest d:DataContext?

```xml
d:DataContext="{d:DesignInstance Type=vm:MainViewModel, IsDesignTimeCreatable=True}"
```

`d:DataContext` to atrybut **tylko dla design-time** (prefiks `d:` = design). Mówi Visual Studio / Blendowi, jakiego ViewModel użyć do podglądu XAML w designerze. Dzięki `IsDesignTimeCreatable=True` designer tworzy instancję `MainViewModel` w tle i pokazuje dane demo. Nie ma żadnego wpływu na działanie aplikacji w runtime.
