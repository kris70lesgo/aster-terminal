#!/bin/sh
set -eu
source_file=$1
output_file=$2
target_rid=${3:-}
mkdir -p "$(dirname "$output_file")"
case "$(uname -s)" in
  Darwin)
    case "$target_rid" in
      ''|osx-arm64) arch=arm64 ;;
      osx-x64) arch=x86_64 ;;
      *) echo "Build Unix PTY helper on its target OS: $target_rid" >&2; exit 1 ;;
    esac
    cc -std=c11 -O2 -Wall -Wextra -arch "$arch" -mmacosx-version-min=13.0 "$source_file" -lutil -o "$output_file"
    ;;
  Linux)
    case "$target_rid" in ''|linux-*) ;; *) echo "Build Unix PTY helper on its target OS: $target_rid" >&2; exit 1 ;; esac
    cc -std=c11 -O2 -Wall -Wextra "$source_file" -lutil -o "$output_file"
    ;;
  *) echo 'Unix helper requires macOS or Linux' >&2; exit 1 ;;
esac
