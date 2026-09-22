# ApiGuard — Proje Planı

> Bu dosya, sohbet geçmişi kaybolsa bile projeye kaldığın yerden devam edebilmen için var.
> Yeni bir Claude Code oturumu açtığında ilk iş bu dosyayı okumak/okutmaktır.

## Ne yapıyoruz ve neden

**Fikir:** GitHub Action — PR'larda OpenAPI/Swagger spec'ini (base branch'teki eski hal vs PR'daki yeni hal)
otomatik karşılaştırıp "breaking change" (kırıcı değişiklik) tespit eder. Örnek: endpoint silinmiş,
yeni zorunlu parametre eklenmiş, response'tan alan kaldırılmış, tip değişmiş vs. PR'a otomatik yorum
bırakır / CI'ı kırar.

**Neden bu fikir:** Backend/API takımlarının gerçek acısı, açık kaynakta iyi bir çözüm yok, .NET ile
hızlı yazılabilir, bir kere kurulan CI aracı genelde silinmez (yüksek "tutma şansı"), bakım maliyeti düşük.

**Kullanıcı bağlamı (efekaan267@gmail.com):** .NET geliştirici, pazarlama yeteneği yok, hızlı ilerlemek
istiyor. Strateji: **önce ücretsiz GitHub Action olarak yayınla** (GitHub Marketplace'te Action'lar için
native ödeme sistemi yok, ama organik arama trafiği var — pazarlama gerektirmiyor). Gerçek kullanım/yıldız
görülürse **GitHub App'e çevirip GitHub Marketplace'in ücretli plan sistemine bağla** (bunun için küçük bir
web servisi hostlamak gerekecek). Bu ilk deneme tutarsa, aynı dağıtım modeliyle (GitHub Action → App)
benzer başka niş fikirler denenecek.

## Mimari

```
apiguard/
  src/
    ApiGuard.Core/    → Diff mantığının tamamı (framework'ten bağımsız, test edilebilir)
      BreakingChange.cs   → BreakingChange record + ChangeSeverity enum (Breaking/Warning)
      OpenApiDiffer.cs    → OpenApiDiffer.Compare(oldDoc, newDoc) statik metod
    ApiGuard.Cli/      → Konsol giriş noktası (ileride Docker container action'ın içinde çalışacak)
      Program.cs          → apiguard <old-spec-path> <new-spec-path>, exit code 1 = breaking change var
  tests/
    ApiGuard.Tests/    → xUnit testleri (OpenApiDifferTests.cs)
```

- .NET 9, `Microsoft.OpenApi.Readers` paketi ile spec parse ediliyor.
- Solution dosyası: `apiguard.slnx` (yeni XML-siz format, Visual Studio ile oluşturuldu).

## Şu ana kadar yapılanlar (durum: MVP diff engine hazır, test edildi)

- [x] Solution + 3 proje (Core, Cli, Tests) kuruldu, referanslar bağlandı.
- [x] `OpenApiDiffer.Compare` şu kuralları kontrol ediyor:
  - Endpoint (path) silinmiş → Breaking
  - Operation (method) silinmiş → Breaking
  - Yeni zorunlu (required) parametre eklenmiş → Breaking
  - Parametre optional'dan required'a geçmiş → Breaking
  - Parametre tipi değişmiş → Breaking
  - Request body required olmuş → Breaking
  - Request body'de yeni zorunlu alan / tip değişikliği → Breaking
  - Response status code silinmiş → Warning
  - Response body'den alan silinmiş / tipi değişmiş → Breaking
- [x] 5 xUnit testi yazıldı, hepsi geçiyor (`dotnet test`).
- [x] Git repo init edildi, `.gitignore` eklendi. **Henüz commit atılmadı.**
- [ ] Henüz GitHub'a push edilmedi (repo yok).

## Sıradaki adımlar (öncelik sırasıyla)

1. **İlk commit'i at** (kullanıcı onayı ile — henüz sorulmadı/istenmedi).
2. **Dockerfile + action.yml** yaz → `ApiGuard.Cli`'yi GitHub Action olarak paketle
   (composite action değil, Docker container action; .NET runtime image kullan).
3. **GitHub PR entegrasyonu:** `Program.cs`'e GitHub API ile PR'a yorum bırakma özelliği ekle
   (`GITHUB_TOKEN`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_PATH` env değişkenlerinden PR bilgisini oku).
4. **Gerçek bir repo üzerinde test et** — iki farklı OpenAPI spec versiyonuyla dene, Action'ın PR'da
   doğru çalıştığını doğrula.
5. **GitHub Marketplace'e yayınla** (ücretsiz Action olarak) — public repo aç, README yaz, tag'le.
6. **Geri bildirim/kullanım topla** (yıldız, issue, kurulum sayısı).
7. **Tutarsa:** GitHub App'e çevir, Marketplace'in ücretli plan sistemine bağla (bu adımda ASP.NET Core
   ile küçük bir webhook servisi hostlamak gerekecek — Azure Container Apps / Fly.io / Railway gibi ucuz
   bir yerde).
8. **Tutarsa (tekrar):** aynı dağıtım modeliyle benzer yeni fikirler dene (örn: GraphQL schema diff,
   gRPC/protobuf breaking change detector gibi aynı paterni tekrar eden ürünler).

## Nasıl devam edilir (yeni oturumda)

```bash
cd "C:\Users\efeka\OneDrive\Belgeler\apiguard\apiguard"
dotnet test        # her şeyin hala çalıştığını doğrula
```

Sonra bu dosyadaki "Sıradaki adımlar" listesinden kaldığın maddeye devam et.
