# Edytor Wielokątów Krzywoliniowych - Dokumentacja

## 1. Instrukcja Obsługi

### Sterowanie Myszyną
* **Kliknięcie lewym przyciskiem myszy w wierzchołek / punkt kontrolny:** Zaznacza pojedynczy wierzchołek lub punkt kontrolny Béziera. Kliknięcie i przeciągnięcie umożliwia jego przemieszczanie; solver ograniczeń automatycznie zaktualizuje powiązane kształty.
* **Kliknięcie lewym przyciskiem myszy w krawędź:** Zaznacza lub odznacza krawędź. Możliwe jest zaznaczenie wielu krawędzi jednocześnie (wymagane np. przy więzach prostopadłości).
* **Kliknięcie lewym przyciskiem myszy we wnętrze wielokąta:** Kliknięcie i przeciągnięcie wewnątrz wielokąta pozwala przesunąć całą figurę.
* **Kliknięcie lewym przyciskiem myszy w pustą przestrzeń:** Usuwa wszelkie bieżące zaznaczenia wierzchołków i krawędzi.

### Skróty Klawiszowe i Pasek Narzędzi
* **Dodaj wierzchołek:** Dodaje nowy wierzchołek w połowie długości zaznaczonej krawędzi.
* **Usuń wierzchołek:** Usuwa zaznaczony wierzchołek i scala przylegające do niego krawędzie (wielokąt musi zachować co najmniej 3 wierzchołki).
* **Ustaw długość:** Otwiera okno dialogowe umożliwiające określenie dokładnej docelowej długości dla zaznaczonej prostej krawędzi.
* **Usuń ograniczenie:** Usuwa więzy długości oraz prostopadłości z zaznaczonych krawędzi.
* **Ustaw prostopadłość:** Wymusza relację prostopadłości (90 stopni) pomiędzy dwoma zaznaczonymi prostymi krawędziami.
* **Przełącz Bezier:** Przekształca zaznaczone krawędzie w kubiczne krzywe Béziera (wraz z uchwytami kontrolnymi i domyślną ciągłością).
* **Przełącz Łuk:** Przekształca zaznaczone krawędzie w łuki okręgu.
* **Przyciski ciągłości (`G0`, `G1`, `C1`):** Przypisuje klasy ciągłości geometrycznej lub parametrycznej do zaznaczonego wierzchołka połączonego z krzywymi lub łukami.
* **Tryb rysowania linii (Przyciski radiowe / Radio Buttons):** Pozwala na natychmiastowe przełączanie pomiędzy wektorowym rendererem bibliotecznym WPF (`LineGeometry`) a niestandardowym, opartym na liczbach całkowitych **algorytmem Bresenhama** (który renderuje ostre, pikselowe schodki dla prostych krawędzi).

---

## 2. Założenia Projektowe

* **Struktura wielokąta:** Wielokąt musi być zamkniętą pętlą uporządkowanych wierzchołków połączonych krawędziami. Każda figura musi zawierać co najmniej 3 wierzchołki.
* **Modułowość krawędzi:** Krawędzie mogą dynamicznie zmieniać swój typ (`Line` – linia, `Bezier` – krzywa Béziera lub `Arc` – łuk).
* **Interaktywne ograniczenia:** System zakłada, że rozwiązywanie więzów geometrycznych powinno odbywać się w czasie rzeczywistym podczas przeciągania wierzchołków.
* **Liczba krawędzi w wielokącie** Liczba możliwych krawędzi w wielokącie wynosi 15.
* **Zapewnienie ciągłości:** Płynne przejścia pomiędzy połączonymi segmentami krzywych są zarządzane za pomocą klas ciągłości G0, G1 oraz C1.

---

## 3. Model Danych

Główna architektura opiera się na wzorcu MVVM (Model-View-ViewModel) oraz kluczowych modelach domenowych:

* **`Polygon`:** Reprezentuje zamknięty kształt 2D zawierający kolekcję `ObservableCollection<Vertex>` (wierzchołki) oraz `ObservableCollection<Edge>` (krawędzie).
* **`Vertex`:** Reprezentuje punkt 2D (`X`, `Y`) wraz ze stanem zaznaczenia (`IsSelected`) oraz metadanymi ciągłości (`ContinuityType`, `IsContinuityExplicitlySet`).
* **`Edge`:** Łączy wierzchołek początkowy (`StartVertex`) z końcowym (`EndVertex`). Przechowuje typ krawędzi (`EdgeType`: `Line`, `Bezier`, `Arc`), opcjonalne punkty kontrolne Béziera (`ControlPoint1`, `ControlPoint2`), referencję do macierzystego wielokąta oraz kolekcję zastosowanych obiektów ograniczeń (`Constraint`).
* **`Constraint`:** Służy jako baza dla konkretnych reguł geometrycznych, zaimplementowanych głównie jako `LengthConstraint` oraz `PerpendicularConstraint` .

---

## 4. Zaimplementowane Algorytmy

### A.`ConstraintSolver`
`ConstraintSolver` pilnuje, że każdy wierzchołek i krawędź jest zgodna z ograniczeniami nałożonymi na nie
* Gdy użytkownik przeciąga wierzchołek, solver dostosowuje połączone krawędzie tak, aby spełnić aktywne ograniczenia (takie jak stałe długości czy kąty proste).
* W przypadku więzów długości, rzutuje wierzchołek końcowy wzdłuż wektora kierunkowego, aby dopasować docelową długość.
* W przypadku więzów prostopadłości, iteracyjnie obróci wektory krawędzi, aby zachować kąt $90^\circ$ względem krawędzi docelowej.

### B. Algorytm Bresenhama dla Prostej
* `sx` i `sy` to nasz krok w zależności, żeby uniezależnić się od kierunku linii. 
* W każdej iteracji pętli obliczamy err = dx - dy (dla dx = x1 - x0 i dy = y1 - y0). W zależnosci od rozmiaru err podejmujemy decyzję czy iść do na ukos czy na bok.
* Nasz err symbolizuje aktualną odległość naszej dyskretnej linii od tej prawdziwej (analitycznej), inaczej err to odległość wartoścu funkcji liniowej w tym punkcie od środka pixelu.

### C. Geometria Łuku Kołowego (`CreateArcGeometry`)
* Oblicza długości cięciw oraz weryfikuje wymagania dotyczące ciągłości (`G1`).
* Wykorzystuje iloczyny wektorowe i skalarne (`Vector.CrossProduct`, operacje na wektorach) do wyznaczania precyzyjnych promieni okręgów oraz kierunków zakrzywienia (`sweep direction`), gwarantując gładkie, styczne przejścia z prostych odcinków lub krzywych w łuki kołowe.

### D. Przyrostowe Wyznaczanie kubicznych Krzywych Béziera
* Konwertuje wielomianową bazę Bernsteina na przyrostowe kroki różnicowe (`PolyLineSegment`) na bazę wielomianową, żeby później za pomocą metody przyrostowej obliczać punkty krzywej.
* Metoda przyrostowa pozwala na obliczanie następnych punktów krzywej beziera za pomocą wartości stałych nie zależych od parametrów funkcji (obliczając 3 pochodną z delty).