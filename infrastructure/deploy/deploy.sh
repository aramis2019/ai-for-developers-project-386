#!/bin/sh
# Деплой Meetly. Вызывается Jenkins-ом по SSH: ключ jenkins-meetly-deploy
# в authorized_keys ограничен forced-command на этот скрипт (шелл и другие
# команды по этому ключу недоступны).
#
# Рабочая копия — /home/development/meetly/deploy.sh на VPS.
set -eu
cd /home/development/meetly
docker compose pull -q
docker compose up -d
docker ps --filter name=meetly-app --format '{{.Names}} {{.Status}}'
