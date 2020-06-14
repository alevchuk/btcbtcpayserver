#!/bin/bash
# set -u
# set -e

exclude=".git|zzz_make_bitcoin_only.sh"
find . -type f | grep -vE "$exclude" | xargs perl -pi -e 's/cryptocurrency/bitcoin/gi'
find . -type f | grep -vE "$exclude" | xargs perl -pi -e 's/crypto/bitcoin/gi'
