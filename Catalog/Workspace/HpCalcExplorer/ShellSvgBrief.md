# HP Calculator Shell SVG — Agent Handoff Brief

Konsolide notlar (code agent + kullanıcı görüşmesi). **Bu dosya SVG/gövde agent'ı içindir.** Tuşlar ve display içeriği bu kapsamın **dışında**.

---

## 1. Proje ve hedef

- **Proje:** TeoCalc (`D:\$Board\Works\Side.Codes\TeoCalc`)
- **Amaç:** Vintage HP hesap makineleri için hafif, parametrik gövde grafikleri
- **Öncelik model:** HP-65
- **Yaklaşım:** **B — sıfırdan parametrik çizim** (PNG/SVG trace dosyalarını sadeleştirmek değil)

### Chrome kaynağı (2026-03 güncelleme)

**SVG shell denemesi sonuç vermedi** (otomatik trace dalgalı; parametrik barrel JPG’ye yaklaşmadı).

| Katman | Kaynak | Not |
|--------|--------|-----|
| Dış gövde chrome | **`hp65_470.png`** (470×870) | Birincil görsel referans; `Hp65FaceplateArt` overlay |
| İç boşluk | Runtime mask (14,48)–(456,862) | Tuşlar, display, switch procedural |
| Tuşlar | `CalcButton` | SVG yok |
| `shell-classic.svg` | **Durduruldu** | İleride yalnızca mask/KML metadata için düşünülebilir |

PNG = foto kalitesi; SVG = parametrik hedef (şimdilik ertelendi).

### Kesin kararlar

| Konu | Karar |
|------|--------|
| `05_C.svg` (Desktop HP-65, ~338 KB, 143 path) | **Doğrudan kopyalama yok** — trace gürültüsü; dış kontur **sadeleştirilerek** referans alınır (`approxPolyDP`, ~8–24 nokta) |
| Tuşlar | **SVG yok** — runtime `CalcButton` (ImGui procedural) |
| Display LED rakamları | **SVG yok** — `ClassicLedDisplayRenderer` / procedural |
| Gövde chrome | **Minimal SVG** — dış gövde eğrisi + footer şeridi + HP logo + display bezel; **evenodd delikler** |
| Tuş etiketleri (f, g, ENTER…) | **Runtime metin** — SVG `<text>` yok |

Code agent tuş tarafında palet + basma efektlerini güncelledi (`CalcChassisPalette`, `CalcButton`). SVG agent gövdeye odaklanır.

---

## 2. Mevcut TeoCalc mimarisi (katmanlar)

### Hedef çizim sırası (SVG v2 — hp65.png referans)

```
shell-body = dış gövde − tek faceplate deliği − footer metin deliği
  └ faceplate-contour: iç kontur (dış eğriye paralel, düz rounded-rect DEĞİL)
  └ card-slot-recess, grooves, footer-strip (boyalı chrome)
runtime: display, switch, keypad, footer text — faceplate deliği içinde
```

**T-şekli sorunu (v1):** Display + slider-band + keypad delikleri birbirine temas edince evenodd birleşip geniş T oluşturuyordu. **v2:** yalnızca `hole-faceplate` kesilir; panel rect’leri dokümantasyon.

**Kontur:** Dış ve iç çerçeve `hp65.png` siluetinden sol/sağ profil çıkarımı + `{FRAME_INSET}px` paralel inset (05_C değil).

### Bugün (geçiş)

```
┌─────────────────────────────────────────┐
│  hp65_470.png overlay (üstte)           │  ← Hp65FaceplateArt — iç maskeli
│  ┌───────────────────────────────────┐  │
│  │ CalcChassisRenderer (procedural)  │  │
│  │ CalcButton × N                    │  │
│  │ LED digits                        │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

İlgili kod:

| Dosya | Rol |
|-------|-----|
| `TeoCalc.Rendering/CalcChassisGeometry.cs` | Referans ölçüler (470×870), display/keypad norm rect |
| `TeoCalc.Rendering/CalcFaceplateLayout.cs` | 35 tuş grid (Classic), hücre span |
| `TeoCalc.Rendering/CalcChassisRenderer.cs` | Procedural gövde çizimi |
| `TeoCalc.Rendering/CalcButton.cs` | Procedural tuş (basma: kayma + tint + skirt) |
| `TeoCalc.Rendering/Hp65FaceplateArt.cs` | `hp65_470.png` yükler, içini maskeler, overlay çizer |
| `TeoCalc.Rendering/CalcFaceplateView.cs` | Hepsini birleştirir |

**Hedef asset yolu (öneri):**

`TeoCalc/Resource/Engine/HP-65/Assets/shell-classic.svg`

Geçiş: SVG hazır olunca code agent `Hp65FaceplateArt` veya benzeri loader'ı PNG → SVG'ye taşır. Mask koordinatları aynı kalır.

---

## 3. Ölçü sözleşmesi (KML + C# — tek kaynak)

Panamatik skin formatı. HP-65 referansı:

**Dosya:** `Catalog/Workspace/HpCalcExplorer/Reference/Panamatik New/HP-65/HP65_470.kml`

```text
IMAGE hp65_470.png TRANSPARENT
BUTTONS 91,258 378,780
DISPLAY 68,54 410,96
```

**C# karşılığı** (`CalcChassisGeometry.cs`):

| Sabit | Değer |
|-------|-------|
| `ReferenceWidth` | 470 |
| `ReferenceHeight` | 870 |
| `DisplayNorm` | x=68/470, y=54/870, w=342/470, h=42/870 |
| `KeypadNorm` | x=91/470, y=258/870, w=287/470, h=522/870 |
| Tuş piksel | 47×57, gap H=10 V=8 |
| `FooterHeightPx` | 36 |
| Slider band | y 96…258 (norm) |

**İç boşluk (eski PNG maskesi)** — geçiş referansı; SVG v1’de tek dikdörtgen yerine **üç delik**:

| Delik (gerçek kesim) | Açıklama |
|----------------------|----------|
| `hole-faceplate` | Tek iç boşluk — dış kontura paralel eğri (hp65.png profil) |
| `hole-footer-text` | Alt şeritte model adı metni |

| Panel (dokümantasyon, kesilmez) | hp65.png ölçümü |
|--------------------------------|-----------------|
| `hole-display` | 58,54 — 414,98 |
| `hole-slider-band` | 58,100 — 412,242 (display ile aynı genişlik) |
| `hole-keypad` | KML 91,258 — 378,780 (CalcButton grid) |

**SVG’de boyalı chrome (delik değil):**

| Öğe | Açıklama |
|-----|----------|
| `faceplate-contour` | Gümüş iç trim (jpg’deki kontur çizgisi) |
| `body-outer-edge` | Dış gövde kenar vurgusu |
| `card-entry-top` | Üst kart giriş yarığı |
| `card-side-groove-top/bottom` | Sağ yandaki 2 kart giriş çizgisi |
| `footer-strip` + `hp-logo` | Alt gümüş şerit + logo |

Eski tek mask (`Hp65FaceplateArt`): sol üst (14,48), sağ alt (456,862) — slider bandı ve faceplate grain hâlâ procedural; gövde dolgusu artık SVG’de.

SVG: `viewBox="0 0 470 870"`. Delikler **şeffaf**; chrome dışarıda kalır.

---

## 3b. SVG naming & documentation (English only)

All identifiers and inline documentation **inside the SVG file** must be **English** (kebab-case IDs, short XML comments). The brief/handoff doc may stay bilingual; the asset itself is English for code review and TeoCalc integration.

### Rules

| Element | Convention | Example |
|---------|------------|---------|
| Root group | `id` + optional `data-*` | `id="hp65-shell-classic"` |
| Layer groups | kebab-case, noun phrase | `body-shell`, `display-bezel` |
| Paths / shapes | descriptive `id` if referenced | `frame-outer`, `footer-strip` |
| `<defs>` | English id | `gradient-frame-edge` |
| XML comments | English, one line | `<!-- Transparent hole: procedural faceplate -->` |
| `class` | optional, match token names | `class="frame-fill"` |
| **Avoid** | Turkish IDs, spaces, `Layer 1` | — |

Do **not** embed user-visible label text in the shell SVG (no `HEWLETT-PACKARD` unless explicitly wanted later — code draws footer text today).

### Recommended layer tree (`shell-classic.svg`)

```xml
<svg viewBox="0 0 470 870" id="hp65-shell-classic" data-model="HP-65" data-family="classic">
  <defs><!-- gradient-frame-fill, gradient-footer-fill --></defs>

  <g id="chrome-static">
    <path id="shell-body" fill-rule="evenodd" ... />  <!-- outer + holes in one path -->
    <path id="display-bezel-ring" fill="none" stroke="..." />
    <path id="footer-strip" ... />
    <g id="hp-logo">...</g>
  </g>

  <g id="holes-runtime">  <!-- documentation rects; holes already in shell-body -->
    <rect id="hole-display" ... />
    <rect id="hole-keypad" ... />
    <rect id="hole-footer-text" ... />
  </g>

  <g id="card-slot"><!-- HP-65/67 runtime chrome --></g>
  <g id="slider-band"><!-- runtime switches --></g>
</svg>
```

### `data-*` hints for future code (optional)

| Attribute | Value | Meaning |
|-----------|-------|---------|
| `data-model` | `HP-65` | Target model |
| `data-family` | `classic` | Shell profile |
| `data-interior-x` | `14` | Transparent region left (px) |
| `data-interior-y` | `48` | Transparent region top (px) |
| `data-interior-width` | `442` | 456 − 14 |
| `data-interior-height` | `814` | 862 − 48 |

Code agent may read these later; not required for v1 but helpful.

---

## 4. Renk paleti (minimal — hp65 foto referansı)

`05_C.svg` renklerini **kullanma** (display'deki açık mavi trace artefaktı). Gerçek HP-65: koyu kırmızı LED penceresi, olive-charcoal gövde, sıcak gri tuşlar.

### Gövde SVG için (6–8 fill yeterli)

| Token | Hex | Kullanım |
|-------|-----|----------|
| `frame` | `#4A4C48` | Dış gövde (olive charcoal) |
| `frame-edge` | `#3A3C38` | Dış kontur gölgesi |
| `footer` | `#B0AEA8` | Alt gümüş şerit (HEWLETT·PACKARD) |
| `footer-ink` | `#302E2A` | Footer yazı (opsiyonel — code da çizebilir) |
| `bezel` | `#0C0C0E` | Display dış çerçevesi (ince; iç cam procedural) |
| `trim-dark` | `#141410` | İnce ayırıcı çizgiler |

Tuş/display renkleri SVG'de **yok** — `CalcChassisPalette.cs` code tarafında.

### Code tarafı palet (referans, SVG ile uyumlu tut)

`TeoCalc.Rendering/CalcChassisPalette.cs` — frame `74,76,72`, faceplate koyu, display glass `26,8,12`, grey keys sıcak ton.

---

## 5. HP-65 fiziksel layout (SVG'ye tuş koyma — sadece bilgi)

35 tuş, 4 geometrik tip (hepsi `CalcButton` runtime):

| Tip | Satırlar | Adet |
|-----|----------|------|
| Küçük (sm) | 1–3, 5 sütun | 15 |
| Standart (md) | 4–7, 4 sütun | 16 |
| ENTER | 2× yükseklik, sağ | 1 |
| Özel (R/S siyah vb.) | — | stil code'da |

SVG agent **tuş çizmeyecek**; keypad alanı mask içinde boş kalır.

Display'deki yatay çizgiler (PNG trace): **yok say** — tek display penceresi + procedural LED.

---

## 6. Model aileleri (ileride shell paylaşımı)

TeoCalc `HpCalcModelCatalog`: 20 model (HP-01 … HP-80).

Panamatik `Reference/Panamatik New/` içinde şu an görseller:

| Klasör | Örnek görsel | Gövde h/w oranı |
|--------|--------------|-----------------|
| HP-65 | hp65_470.png (470×870) | **1.85** Classic büyük |
| HP-67BE | HP67_330.png (330×620) | 1.88 |
| HP-70 | hp70_330.png (330×620) | 1.88 (67 ile aynı şablon) |
| HP-34 | HP34c_800.png | 1.88 Spice |
| HP-25 | HP25_360.png (360×683) | 1.90 Woodstock |
| HP-19 | HP19C_414.jpg | 1.91 |
| HP-29 | HP-29C.png | 1.93 en dar/uzun |

**~%4–5 oran farkı** — tıpatıp aynı değil ama 2–3 shell profili yeterli:

1. **`shell-classic.svg`** — HP-35,45,55,65,67,70,80 (viewBox 470×870 veya normalize 1×1.851)
2. **`shell-compact.svg`** — HP-21–38, 29C (viewBox 360×683 civarı; KML'den ölçü al)
3. **Ayrı** — HP-01 (saat), HP-19C (yazıcı formu)

İlk teslim: **sadece Classic / HP-65**. Compact ikinci faz.

HP Museum referans: https://www.hpmuseum.org/ (Classic ~15.1×8 cm, Woodstock ~12.7×6.5 cm).

---

## 7. Referans görseller (yerel)

| Yol | Not |
|-----|-----|
| `Catalog/Workspace/HpCalcExplorer/Reference/Panamatik New/HP-65/hp65_470.png` | Panamatik skin — **birincil chrome referansı** |
| `Catalog/Workspace/HpCalcExplorer/Reference/Panamatik New/HP-65/HP65_470.kml` | Piksel layout |
| `C:\Users\ilkay\Desktop\HP-65\hp65.jpg` | Yüksek kaliteli foto (renk doğrulama) |
| `C:\Users\ilkay\Desktop\HP-65\hp65.png` | Temiz ön yüz |
| `C:\Users\ilkay\Desktop\HP-65\05_C.svg` | **Anti-pattern** — trace, kullanma |

Desktop `05_A/B/C.svg`: potrace denemeleri; ignore.

---

## 8. SVG agent teslim kriterleri

### İlk deliverable: `shell-classic.svg` (HP-65)

- [x] `viewBox="0 0 470 870"`
- [x] Dış gövde: `05_C` silueti, **yuvarlatılmış köşeler** (Q arc)
- [x] evenodd delikler: display, card-slot, slider-band, keypad, footer-text
- [x] Display: **yalnızca boşluk** — kalın siyah bezel kaldırıldı
- [x] Kontur çizgileri: `faceplate-contour`, `body-outer-edge`
- [x] Kart: üst giriş + sağ yan 2 groove + `hole-card-slot`
- [x] Switch bandı: `hole-slider-band` + doc switch rect’leri
- [x] Footer şeridi düzeltildi (rounded rect, alt taşma yok)
- [ ] Tuş yok, slider thumb yok, kart yuvası chrome opsiyonel (`card-slot` boş grup)
- [x] English `id` / comments; `chrome-static`, `holes-runtime`, …
- [x] Dosya boyutu < 8 KB (v1 ~3 KB)

### Yapma listesi

- 35 tuş path'i
- PNG trace'den kopyala-yapıştır path
- Display içi yatay şeritler (artefakt)
- 85 renk / 8 ondalık koordinat
- SVG `<text>` ile tuş etiketleri
- SMIL animasyon (gerek yok)

### Opsiyonel ikinci dosya

`shell-classic-mask.svg` — sadece dış kontur + delik (alpha). Code zaten runtime mask uyguluyor; tek dosyada şeffaf iç de yeterli.

---

## 9. Code agent sonraki adımlar (SVG hazır olunca)

1. `shell-classic.svg` → `Resource/Engine/HP-65/Assets/`
2. `Hp65FaceplateArt` PNG yerine SVG rasterize (Skia `SKSvg`) veya vektör overlay
3. Mask sabitleri doğrula / gerekirse SVG iç boşluğu ile hizala
4. `shell-compact.svg` + `CalcChassisGeometry` ikinci profil (HP-25 KML)

---

## 10. Tuş tarafı özeti (SVG agent dokunmaz)

`CalcButton` basma efektleri (code'da uygulandı):

1. Aşağı kayma (`PressTravelRatio`)
2. Basılıyken üst highlight kapalı
3. Skirt koyulaşması + üst yarı saydam tint

Tuş renkleri: `CalcButtonStyle` (Black / Grey / Orange / Blue) + `CalcChassisPalette`.

---

## 11. Kısa prompt (diğer agent'a kopyala)

```
TeoCalc HP-65 shell SVG. No keys, no LED digits.

Brief: TeoCalc/Catalog/Workspace/HpCalcExplorer/ShellSvgBrief.md
Reference: .../Panamatik New/HP-65/hp65_470.png
Silhouette: Desktop/HP-65/05_C.svg → simplify outer contour only (do not paste trace paths)

viewBox 0 0 470 870. evenodd holes: display (68,54,342,42), keypad (91,258,287,522), footer text (48,826,374,28).
Paint: outer body curve + footer strip + HP logo + display bezel stroke. <8KB.
Colors: frame #4A4C48, footer #B0AEA8, bezel #141410.
English only inside SVG (kebab-case ids).
```

---

*Oluşturulma: code agent oturumu konsolidasyonu. Sorular için code agent'a dön.*
