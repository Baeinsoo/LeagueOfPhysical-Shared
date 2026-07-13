#!/bin/bash
set -e  # 실패 시 중단

# generated 폴더 초기화
echo "Cleaning up generated folder..."
rm -rf ../Runtime.Generated/Scripts/Protobuf
# MessageIds.cs는 지우지 않는다 — generate_message_ids.sh가 기존 파일을 읽어 ID를 보존한다(안정 wire 계약).
# 지우면 모든 MessageId가 재번호돼 클·서/배포본과 wire desync가 난다. (아래 MessageInitializer는 파생물이라 재생성 안전.)
rm -f  ../Runtime.Generated/Scripts/MessageInitializer.cs
rm -f  ../Runtime.Generated/Scripts/MessageInitializer.cs.meta
mkdir -p ../Runtime.Generated/Scripts/Protobuf

./compile_protos.sh
./generate_imessage.sh
./generate_message_ids.sh
./generate_message_initializer.sh

echo "All proto-related scripts executed successfully."
