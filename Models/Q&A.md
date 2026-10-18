# Q&A - Models

## Pytanie: Po co mi OnPropertyChanged?

`OnPropertyChanged` jest niezbêdna dla powi¹zania danych (data binding) w WPF. Kiedy zmienia siê w³aœciwoœæ `Position` lub `Continuity`, metoda powiadamia interfejs u¿ytkownika o tej zmianie przez zdarzenie `PropertyChanged`. Bez tego WPF nie wie, ¿e model zmieni³ siê, i UI nie zostanie zaktualizowany. Dziêki temu powi¹zanie danych dzia³a automatycznie — zmiana w modelu natychmiast pojawia siê w interfejsie u¿ytkownika.

## Pytanie: Po co mi ObservableCollection zamiast List?

`ObservableCollection<T>` automatycznie powiadamia UI o ka¿dej zmianie w kolekcji: dodaniu, usuniêciu czy zmianie kolejnoœci elementów. `List<T>` tego nie robi — dodanie wierzcho³ka do `_vertices` nie odœwie¿y interfejsu. W WPF `ObservableCollection<T>` jest standardem dla kolekcji w UI (np. ListBox, DataGrid), poniewa¿ zapewnia, ¿e zmiany danych natychmiast odzwierciedlaj¹ siê w oknie. Bez niej musisz rêcznie wywo³ywaæ `OnPropertyChanged`, co jest podatne na b³êdy.