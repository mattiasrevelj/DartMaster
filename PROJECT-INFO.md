# DartMaster - Projektinformation & Setup Guide

**Version:** 1.0  
**Senast uppdaterad:** 2026-02-12  
**Status:** Under utveckling (MVP)

---

## 📋 Innehållsförteckning
1. [Projektöversikt](#projektöversikt)
2. [Miljööversikt](#miljööversikt)
3. [Kubernetes Setup](#kubernetes-setup)
4. [Docker & MariaDB](#docker--mariadb)
5. [Database](#database)
6. [Utvecklingsmiljö](#utvecklingsmiljö)
7. [Användbara Kommandon](#användbara-kommandon)
8. [Nästa Steg](#nästa-steg)

---

## 🎯 Projektöversikt

### DartMaster - Dartturnerings Hanteringssystem

En webbaserad plattform för hantering av dartturnering med live-uppdateringar, matchrapportering och detaljerad statistik.

**Omfattning:**
- Upp till 100 spelare per turnering
- Gruppspelsformat (MVP)
- 301-format (MVP), 501 senare
- 1-6 spelare per match
- Live-scoreboard för åskådare
- Push- och email-notifikationer

**Användarroller:**
- Admin
- Spelare
- Åskådare (icke-aktiva spelare)

**Språk:** Svenska & Engelska

---

## 🌍 Miljööversikt

### Tech Stack

| Komponent | Val | Version |
|-----------|-----|---------|
| **Frontend** | React JS | - |
| **Backend** | C# Minimal API | .NET 8 |
| **Database** | MariaDB | Latest |
| **Authentication** | JWT + Bearer | - |
| **Realtid** | WebSocket (SignalR) | - |
| **Hosting** | Kubernetes | - |
| **Container** | Docker | 29.2.0 |

### Två Kubernetes Kluster

1. **local** (Docker Desktop)
   - Voor lokal utveckling
   - Adress: https://kubernetes.docker.internal:6443
   - Status: ✓ Konfigurerad

2. **dev** (Remote)
   - Utvecklingsserver
   - Adress: https://10.11.15.30:6443
   - Status: ✓ Konfigurerad
   - Nodes: 4 (3 control-plane, 1 worker)
   - Version: Kubernetes 1.32.4+k3s1

---

## ☸️ Kubernetes Setup

### kubeconfig Fil Location
```
C:\Users\Mattias.Revelj\.kube\config
```

### Kontexter
```bash
# Se alla kontexter
kubectl config get-contexts

# Växla till local (Docker Desktop)
kubectl config use-context local

# Växla till dev (Remote)
kubectl config use-context dev

# Se aktiv kontext
kubectl config current-context
```

### Aktuell Setup
- **Standardkontext:** local (Docker Desktop Kubernetes)
- **Remote kluster:** dev (Trinity kluster)
- **Status:** Båda fungerar ✓

### Kubernetes Kommandon
```bash
# Check cluster info
kubectl cluster-info

# Se noder
kubectl get nodes

# Se pods
kubectl get pods -A

# Se namespaces
kubectl get namespaces
```

---

## 🐋 Docker & MariaDB

### Docker Desktop
- **Status:** ✓ Installerat och igång
- **Version:** 29.2.0, build 0b9d198
- **Kubernetes:** Inbyggt Kubernetes-stöd aktivt

### MariaDB Container

**Status:** ✓ Igång i Docker

**Container Information:**
- **Container namn:** dartmaster-db
- **Image:** mariadb:latest
- **Port:** 3306
- **Restart policy:** unless-stopped

**Credentials:**
```
Host: localhost
Port: 3306
Username: dartmaster
Password: dartmaster_pass
Database: dartmaster
Root password: rootpassword
```

**phpMyAdmin:**
- **URL:** http://localhost:8080
- **Username:** dartmaster
- **Password:** dartmaster_pass
- **Server:** mariadb

### Docker Compose File
Location: `C:\Dev\DartMaster\docker-compose.yml`

```bash
# Start containers
docker-compose up -d

# Stop containers
docker-compose down

# View logs
docker-compose logs -f mariadb

# Check status
docker-compose ps

# Restart
docker-compose restart
```

---

## 🗄️ Database

### Schema Location
File: `C:\Dev\DartMaster\database\schema.sql`

### Tabeller

1. **users** - Användarkonton och autentisering
   - Roller: Admin, Player, Spectator
   - Fields: id, username, email, password_hash, role, is_active

2. **tournaments** - Turneringsinformation
   - Formato: Group, Series, Knockout
   - Match format: 301, 501
   - Status: Planning, Active, Completed

3. **tournament_groups** - Grupper inom turnering
   - För gruppspelsformat

4. **tournament_participants** - Deltagare i turnering
   - Status: Registered, Active, Withdrawn, WO (Walk Over)

5. **matches** - Matcher i turnering
   - Status: Scheduled, Live, Waiting for confirmation, Completed
   - Format: 301, 501

6. **match_participants** - Spelare i specifik match
   - 1-6 spelare per match
   - Konfirmeringstatus

7. **dart_throws** - Individuella pilkast
   - Pilnummer, rundenummer
   - Poäng, återstående poäng
   - Dubbel-flagga

8. **match_confirmations** - Resultatbekräftelser
   - Från andra spelare än rapportören

9. **player_statistics** - Spelstatistik per turnering
   - Matcher spelade/vunna/förlorade
   - Win/Loss ratio
   - Genomsnittlig poäng
   - Ranking

10. **notifications** - Användarmeddelandel
    - Typ: Match_Result, Tournament_Start, osv

11. **notification_subscriptions** - Push- och emailabonnemang
    - Web push endpoints
    - Email adress

12. **refresh_tokens** - JWT refresh-tokens
    - För autentisering

### Connection String (C#)
```csharp
"Server=localhost;Port=3306;Database=dartmaster;User Id=dartmaster;Password=dartmaster_pass;"
```

### Databaskonfiguration
```bash
# Backup database
docker exec dartmaster-db mysqldump -u dartmaster -p dartmaster_pass dartmaster > backup.sql

# Restore database
docker exec -i dartmaster-db mariadb -u dartmaster -p dartmaster_pass dartmaster < backup.sql

# Direct SQL
docker exec dartmaster-db mariadb -u dartmaster -p dartmaster_pass dartmaster -e "SHOW TABLES;"
```

---

## 💻 Utvecklingsmiljö

### Installation Status

**Installerat:**
- ✅ Docker Desktop (29.2.0)
- ✅ kubectl (1.34.1)
- ✅ Docker Compose
- ✅ kubeconfig konfigurerad
- ✅ MariaDB igång
- ✅ Windows PowerShell

**Att installera:**
- ⏳ WSL2 (optional, för Linux-miljö)
- ⏳ .NET 8 SDK (för C# backend)
- ⏳ Node.js (för React frontend)
- ⏳ Visual Studio Code extensions

### Projektstruktur
```
C:\Dev\DartMaster\
├── README.md                 # Huvudprojektinfo
├── REQUIREMENTS.md           # Kravspecifikation
├── PROJECT-INFO.md           # Den här filen
├── docker-compose.yml        # Docker Compose för MariaDB
├── database/
│   ├── schema.sql           # MariaDB schema
│   └── README.md            # Databassetupinstruktioner
├── backend/                 # C# Minimal API (att skapas)
│   ├── DartMaster.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   └── ...
├── frontend/                # React JS (att skapas)
│   ├── package.json
│   ├── src/
│   └── ...
└── k8s/                      # Kubernetes manifests (att skapas)
    ├── backend.yaml
    ├── database.yaml
    └── ingress.yaml
```

---

## 🔧 Användbara Kommandon

### Kubernetes

```powershell
# Switch to local cluster
kubectl config use-context local

# Switch to dev cluster
kubectl config use-context dev

# Currently using
kubectl config current-context

# Get nodes
kubectl get nodes

# Get all pods
kubectl get pods -A

# Get services
kubectl get svc -A

# Deploy YAML
kubectl apply -f file.yaml

# View logs
kubectl logs pod-name -f

# Describe pod
kubectl describe pod pod-name

# Get into pod
kubectl exec -it pod-name -- /bin/bash
```

### Docker & Docker Compose

```powershell
# Start containers
docker-compose up -d

# Stop containers
docker-compose down

# View logs
docker-compose logs -f

# Rebuild images
docker-compose build

# Check running containers
docker ps

# View all containers
docker ps -a

# View mages
docker images

# Prune unused resources
docker system prune

# Access container shell
docker exec -it dartmaster-db bash
```

### MariaDB

```powershell
# Access database CLI
docker exec -it dartmaster-db mariadb -u dartmaster -p dartmaster_pass

# Run SQL file
docker exec -i dartmaster-db mariadb -u dartmaster -p dartmaster_pass dartmaster < schema.sql

# Backup
docker exec dartmaster-db mysqldump -u dartmaster -p dartmaster_pass dartmaster > backup.sql

# Show tables
docker exec dartmaster-db mariadb -u dartmaster -p dartmaster_pass dartmaster -e "SHOW TABLES;"

# Check connections
docker exec dartmaster-db mariadb -u dartmaster -p dartmaster_pass dartmaster -e "SHOW PROCESSLIST;"
```

### Git (förslagat)

```bash
# Initialize repo (om inte redan gjort)
git init

# Status
git status

# Add changes
git add .

# Commit
git commit -m "descriptive message"

# Push
git push origin main
```

---

## 🎯 Nästa Steg (Prioriterad ordning)

### Fas 1: Backend Setup
- [ ] Skapa C# Minimal API-projekt
- [ ] Installera NuGet packages (EF Core, JWT, etc)
- [ ] Skapa Entity Framework DbContext
- [ ] Konfigurerar database connection
- [ ] Skapa autentiserings-endpoints (register, login)
- [ ] JWT token-generering

### Fas 2: Core API Endpoints
- [ ] POST/GET /api/tournaments
- [ ] POST/GET /api/matches
- [ ] POST /api/matches/{id}/score (pilkast-registrering)
- [ ] POST /api/matches/{id}/confirm (resultatbekräftelse)
- [ ] GET /api/users/{id}/stats

### Fas 3: Realtid
- [ ] SignalR-integration för live updates
- [ ] Live scoreboard-konekxion
- [ ] Resultatnotifikationer i realtid

### Fas 4: Notifications
- [ ] Push-notifikationer (Web Push API)
- [ ] Email-notifikationer
- [ ] Notification-tabell i DB

### Fas 5: Frontend
- [ ] React-projekt setup
- [ ] Komponentark
- [ ] Login/Register sidor
- [ ] Turneringsdashboard
- [ ] Live-scoreboard

### Fas 6: Deployment
- [ ] Dockerfile för backend
- [ ] Dockerfile för frontend
- [ ] Kubernetes YAML-manifest
- [ ] Deploy till local kluster
- [ ] Deploy till dev kluster

---

## 📝 Konfigurationsfiler

### docker-compose.yml Location
```
C:\Dev\DartMaster\docker-compose.yml
```

### kubeconfig Location
```
C:\Users\Mattias.Revelj\.kube\config
```

### Environment Variables (å initiera senare)

För backend C#:
```
DatabaseUrl=Server=localhost;Port=3306;Database=dartmaster;User Id=dartmaster;Password=dartmaster_pass;
JwtSecret=<very-long-secret-key>
JwtIssuer=DartMaster
JwtAudience=DartMasterUsers
```

---

## ⚠️ Viktigt att Komma Ihåg

1. **Lokalt kluster är standard** - kubectl går till Docker Desktop Kubernetes
   - Byt med: `kubectl config use-context dev` för remote

2. **MariaDB körs i Docker** - Inte installation på maskin
   - Startbar med: `docker-compose up -d`
   - Port: 3306 (localhost)

3. **Två Kubernetes-kluster**
   - `local` = Docker Desktop (för development)
   - `dev` = Remote Trinity-kluster (för staging)

4. **Databaskonfiguration**
   - User: dartmaster
   - Pass: dartmaster_pass
   - Database: dartmaster

5. **Socket/Port-problem?**
   - Kolla att Docker Desktop körs
   - Kolla att docker daemon är igång

6. **kubeconfig finns på:**
   - `C:\Users\Mattias.Revelj\.kube\config`

---

## 🔐 Säkerhet (Att Konfigureras)

- [ ] Change default passwords för produktion
- [ ] Generate strong JWT secret
- [ ] Enable HTTPS/TLS
- [ ] Set up ingress med SSL
- [ ] Configure secrets i Kubernetes
- [ ] Enable RBAC
- [ ] Set up audit logging

---

## 📚 Dokumentreferenser

- [REQUIREMENTS.md](./REQUIREMENTS.md) - Fullständig kravspecifikation
- [database/README.md](./database/README.md) - Databassetupinstruktioner
- [database/schema.sql](./database/schema.sql) - Databasschemat
- [docker-compose.yml](./docker-compose.yml) - Docker Compose-konfiguration

---

## 🆘 Troubleshooting

### MariaDB ansluter inte
```powershell
# Verifiera Docker är igång
docker ps

# Starta containers
docker-compose up -d

# Vänta 10-15 sekunder för MariaDB att initiera

# Testa anslutning
Test-NetConnection localhost -Port 3306
```

### kubectl fungerar inte
```powershell
# Verifiera kontexten är rätt
kubectl config current-context

# Verifiera kubeconfig-fil
cat $env:USERPROFILE\.kube\config

# Testa anslutning
kubectl cluster-info
```

### Port redan i bruk
Redigera `docker-compose.yml`:
```yaml
ports:
  - "3307:3306"  # Använd helt port
```

### WSL-problem
Du behöver inte WSL för att köra docker desktop med Kubernetes lokalt. Docker Desktop har inbyggt Kubernetes.

---

## 📞 Kontaktinfo för Admin

**Remote Kubernetes:**
- Adress: 10.11.15.30:6443
- Kluster: Trinity (3+1 noder)
- Version: 1.32.4+k3s1

**Lokalt Kubernetes:**
- Adress: kubernetes.docker.internal:6443
- Kluster: Docker Desktop (1 nod)

---

**Sist uppdaterad:** 2026-02-12 | **Uppdaterad av:** Mattias Revelj
