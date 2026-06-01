#!/bin/bash
set -e  # 실패 시 중단

# generated 폴더 초기화
echo "Cleaning up generated folder..."
rm -rf ../Runtime.Generated/Scripts/Protobuf
rm -f  ../Runtime.Generated/Scripts/MessageIds.cs
rm -f  ../Runtime.Generated/Scripts/MessageIds.cs.meta
rm -f  ../Runtime.Generated/Scripts/MessageInitializer.cs
rm -f  ../Runtime.Generated/Scripts/MessageInitializer.cs.meta
mkdir -p ../Runtime.Generated/Scripts/Protobuf

./compile_protos.sh
./generate_imessage.sh
./generate_message_ids.sh
./generate_message_initializer.sh

echo "All proto-related scripts executed successfully."
