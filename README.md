# MonoGame FPS Demo

Contoh proyek MonoGame sederhana yang menampilkan mekanik first-person shooter (FPS) dasar:

- Kamera sudut pandang orang pertama dengan kontrol mouse-look.
- Gerakan karakter menggunakan `WASD` dan sprint dengan `Shift`.
- Menembak musuh kubus menggunakan klik kiri atau tombol `Space`.
- Skor dan HUD sederhana dengan crosshair.

## Kontrol

| Aksi            | Tombol                      |
|-----------------|-----------------------------|
| Gerak maju/mundur | `W` / `S`                |
| Gerak kanan/kiri | `D` / `A`                 |
| Sprint           | `Left Shift`              |
| Menembak         | Klik kiri / `Space`       |
| Keluar game      | `Esc`                     |

## Cara Menjalankan

1. Pastikan [.NET 8 SDK](https://dotnet.microsoft.com/download) sudah terpasang.
2. Bangun dan jalankan gim:

```powershell
cd d:\Rustaman\Monogame
dotnet build
dotnet run
```

Musuh akan respawn di lokasi acak setelah ditembak. Selamat bersenang-senang!
