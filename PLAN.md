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
- [x] Git repo init edildi, `.gitignore` eklendi.
- [x] **GitHub'a push edildi:** https://github.com/efekagann/apiguard (public repo, `gh` CLI ile OAuth
      device-flow üzerinden `efekagann` hesabına bağlanıldı).
- [x] `Dockerfile` yazıldı (multi-stage: dotnet SDK ile publish, runtime image ile çalıştır).
- [x] `action.yml` yazıldı (Docker container action, `old-spec-path` / `new-spec-path` inputları).
- [x] `GitHubReporter.cs` eklendi: `GITHUB_TOKEN` + PR event payload'ından PR numarasını okuyup
      breaking/warning listesini PR'a otomatik yorum olarak bırakıyor (sadece `pull_request` event'inde
      çalışır, token yoksa sessizce atlar).
- [x] `README.md` ve `.github/workflows/ci.yml` (push/PR'da `dotnet test`) eklendi.
- [x] `examples/old-spec.yaml` + `examples/new-spec.yaml` — Docker image'ı lokal test etmek için örnek çift.
- [ ] **Docker image lokal build/test edilmedi henüz** — Docker Desktop kullanıcı tarafından güncelleniyordu,
      motor tekrar ayağa kalkması bekleniyor.

## Sıradaki adımlar (öncelik sırasıyla)

1. **Docker image'ı build edip `examples/` altındaki spec çiftiyle lokal doğrula** (Docker Desktop
   güncellemesi bitince devam).
2. **Gerçek bir repo üzerinde uçtan uca test et** — ApiGuard'ı bir PR'da gerçekten çalıştır, PR yorumunun
   doğru göründüğünü doğrula.
3. **v1 tag'i at, GitHub Marketplace'e yayınla** (ücretsiz Action olarak).
4. **Geri bildirim/kullanım topla** (yıldız, issue, kurulum sayısı).
5. **Tutarsa:** GitHub App'e çevir, Marketplace'in ücretli plan sistemine bağla (bu adımda ASP.NET Core
   ile küçük bir webhook servisi hostlamak gerekecek — Azure Container Apps / Fly.io / Railway gibi ucuz
   bir yerde).
6. **Tutarsa (tekrar):** aynı dağıtım modeliyle benzer yeni fikirler dene (örn: GraphQL schema diff,
   gRPC/protobuf breaking change detector gibi aynı paterni tekrar eden ürünler).

## Nasıl devam edilir (yeni oturumda)

```bash
cd "C:\Users\efeka\OneDrive\Belgeler\apiguard\apiguard"
dotnet test        # her şeyin hala çalıştığını doğrula
```

Sonra bu dosyadaki "Sıradaki adımlar" listesinden kaldığın maddeye devam et.
