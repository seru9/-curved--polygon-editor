# Edytor Wielok¹tów Krzywoliniowych - Dokumentacja

## 1. Instrukcja Obs³ugi

### Sterowanie Myszyn¹
* **Klikniêcie lewym przyciskiem myszy w wierzcho³ek / punkt kontrolny:** Zaznacza pojedynczy wierzcho³ek lub punkt kontrolny Béziera. Klikniêcie i przeci¹gniêcie umo¿liwia jego przemieszczanie; solver ograniczeñ automatycznie zaktualizuje powi¹zane kszta³ty.
* **Klikniêcie lewym przyciskiem myszy w krawêdŸ:** Zaznacza lub odznacza krawêdŸ. Mo¿liwe jest zaznaczenie wielu krawêdzi jednoczeœnie (wymagane np. przy wiêzach prostopad³oœci).
* **Klikniêcie lewym przyciskiem myszy we wnêtrze wielok¹ta:** Klikniêcie i przeci¹gniêcie wewn¹trz wielok¹ta pozwala przesun¹æ ca³¹ figurê.
* **Klikniêcie lewym przyciskiem myszy w pust¹ przestrzeñ:** Usuwa wszelkie bie¿¹ce zaznaczenia wierzcho³ków i krawêdzi.

### Skróty Klawiszowe i Pasek Narzêdzi
* **Dodaj wierzcho³ek:** Dodaje nowy wierzcho³ek w po³owie d³ugoœci zaznaczonej krawêdzi.
* **Usuñ wierzcho³ek:** Usuwa zaznaczony wierzcho³ek i scala przylegaj¹ce do niego krawêdzie (wielok¹t musi zachowaæ co najmniej 3 wierzcho³ki).
* **Ustaw d³ugoœæ:** Otwiera okno dialogowe umo¿liwiaj¹ce okreœlenie dok³adnej docelowej d³ugoœci dla zaznaczonej prostej krawêdzi.
* **Usuñ ograniczenie:** Usuwa wiêzy d³ugoœci oraz prostopad³oœci z zaznaczonych krawêdzi.
* **Ustaw prostopad³oœæ:** Wymusza relacjê prostopad³oœci (90 stopni) pomiêdzy dwoma zaznaczonymi prostymi krawêdziami.
* **Prze³¹cz Bezier:** Przekszta³ca zaznaczone krawêdzie w kubiczne krzywe Béziera (wraz z uchwytami kontrolnymi i domyœln¹ ci¹g³oœci¹).
* **Prze³¹cz £uk:** Przekszta³ca zaznaczone krawêdzie w ³uki okrêgu.
* **Przyciski ci¹g³oœci (`G0`, `G1`, `C1`):** Przypisuje klasy ci¹g³oœci geometrycznej lub parametrycznej do zaznaczonego wierzcho³ka po³¹czonego z krzywymi lub ³ukami.
* **Tryb rysowania linii (Przyciski radiowe / Radio Buttons):** Pozwala na natychmiastowe prze³¹czanie pomiêdzy wektorowym rendererem bibliotecznym WPF (`LineGeometry`) a niestandardowym, opartym na liczbach ca³kowitych **algorytmem Bresenhama** (który renderuje ostre, pikselowe schodki dla prostych krawêdzi).

---

## 2. Za³o¿enia Projektowe

* **Struktura wielok¹ta:** Wielok¹t musi byæ zamkniêt¹ pêtl¹ uporz¹dkowanych wierzcho³ków po³¹czonych krawêdziami. Ka¿da figura musi zawieraæ co najmniej 3 wierzcho³ki.
* **Modu³owoœæ krawêdzi:** Krawêdzie mog¹ dynamicznie zmieniaæ swój typ (`Line` – linia, `Bezier` – krzywa Béziera lub `Arc` – ³uk).
* **Interaktywne ograniczenia:** System zak³ada, ¿e rozwi¹zywanie wiêzów geometrycznych powinno odbywaæ siê w czasie rzeczywistym podczas przeci¹gania wierzcho³ków.
* **Zapewnienie ci¹g³oœci:** P³ynne przejœcia pomiêdzy po³¹czonymi segmentami krzywych s¹ zarz¹dzane za pomoc¹ klas ci¹g³oœci G0, G1 oraz C1.

---

## 3. Model Danych

G³ówna architektura opiera siê na wzorcu MVVM (Model-View-ViewModel) oraz kluczowych modelach domenowych:

* **`Polygon`:** Reprezentuje zamkniêty kszta³t 2D zawieraj¹cy kolekcjê `ObservableCollection<Vertex>` (wierzcho³ki) oraz `ObservableCollection<Edge>` (krawêdzie).
* **`Vertex`:** Reprezentuje punkt 2D (`X`, `Y`) wraz ze stanem zaznaczenia (`IsSelected`) oraz metadanymi ci¹g³oœci (`ContinuityType`, `IsContinuityExplicitlySet`).
* **`Edge`:** £¹czy wierzcho³ek pocz¹tkowy (`StartVertex`) z koñcowym (`EndVertex`). Przechowuje typ krawêdzi (`EdgeType`: `Line`, `Bezier`, `Arc`), opcjonalne punkty kontrolne Béziera (`ControlPoint1`, `ControlPoint2`), referencjê do macierzystego wielok¹ta oraz kolekcjê zastosowanych obiektów ograniczeñ (`Constraint`).
* **`Constraint`:** S³u¿y jako baza dla konkretnych regu³ geometrycznych, zaimplementowanych g³ównie jako `LengthConstraint` oraz `PerpendicularConstraint` .

---

## 4. Zaimplementowane Algorytmy

### A.`ConstraintSolver`
`ConstraintSolver` pilnuje, ¿e ka¿dy wierzcho³ek i krawêdŸ jest zgodna z ograniczeniami na³o¿onymi na nie
* Gdy u¿ytkownik przeci¹ga wierzcho³ek, solver dostosowuje po³¹czone krawêdzie tak, aby spe³niæ aktywne ograniczenia (takie jak sta³e d³ugoœci czy k¹ty proste).
* W przypadku wiêzów d³ugoœci, rzutuje wierzcho³ek koñcowy wzd³u¿ wektora kierunkowego, aby dopasowaæ docelow¹ d³ugoœæ.
* W przypadku wiêzów prostopad³oœci, iteracyjnie obróci wektory krawêdzi, aby zachowaæ k¹t $90^\circ$ wzglêdem krawêdzi docelowej.

### B. Algorytm Bresenhama dla Prostej
* Dzia³a ca³kowicie na wspó³rzêdnych ca³kowitoliczbowych (`int`) poprzez zaokr¹glanie pozycji pikseli.
* `sx` i `sy` to nasz krok w zale¿noœci, ¿eby uniezale¿niæ siê od kierunku linii. 
* W ka¿dej iteracji pêtli obliczamy err = dx - dy (dla dx = x1 - x0 i dy = y1 - y0). W zale¿nosci od rozmiaru err podejmujemy decyzjê czy iœæ do na ukos czy na bok.
* Nasz err symbolizuje aktualn¹ odleg³oœæ naszej dyskretnej linii od tej prawdziwej (analitycznej), inaczej err to odleg³oœæ wartoœcu funkcji liniowej w tym punkcie od œrodka pixelu.

### C. Geometria £uku Ko³owego (`CreateArcGeometry`)
* Oblicza d³ugoœci ciêciw oraz weryfikuje wymagania dotycz¹ce ci¹g³oœci (`G1`).
* Wykorzystuje iloczyny wektorowe i skalarne (`Vector.CrossProduct`, operacje na wektorach) do wyznaczania precyzyjnych promieni okrêgów oraz kierunków zakrzywienia (`sweep direction`), gwarantuj¹c g³adkie, styczne przejœcia z prostych odcinków lub krzywych w ³uki ko³owe.

### D. Przyrostowe Wyznaczanie kubicznych Krzywych Béziera
* Konwertuje wielomianow¹ bazê Bernsteina na przyrostowe kroki ró¿nicowe (`PolyLineSegment`) na bazê wielomianow¹, ¿eby póŸniej za pomoc¹ metody przyrostowej obliczaæ punkty krzywej.
* Metoda przyrostowa pozwala na obliczanie nastêpnych punktów krzywej beziera za pomoc¹ wartoœci sta³ych nie zale¿ych od parametrów funkcji (obliczaj¹c 3 pochodn¹ z delty).