#!/usr/bin/env bash
# Linux/macOS'ta Mono ile derleme (apt install mono-devel). Çıktı: dist/PeriyodikKontrolTakip.exe
# .NET Framework 4.5 başvuru API'sine karşı derler; böylece yalnızca Windows'ta bulunan metotlar kullanılır.
set -e
cd "$(dirname "$0")"
A=/usr/lib/mono/4.5-api
mkdir -p dist
mcs -nologo -codepage:utf8 -optimize+ -target:winexe -platform:anycpu \
  -out:dist/PeriyodikKontrolTakip.exe -win32icon:src/app.ico \
  -nostdlib -lib:$A -r:$A/mscorlib.dll -r:$A/System.dll -r:$A/System.Core.dll -r:$A/System.Drawing.dll \
  -r:$A/System.Windows.Forms.dll -r:$A/System.Web.Extensions.dll -r:$A/System.IO.Compression.dll \
  src/*.cs
echo "Derlendi: dist/PeriyodikKontrolTakip.exe"
