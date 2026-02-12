# DartMaster - Kravspecifikation

**Version:** 1.0  
**Datum:** 2026-02-11  
**Status:** Godkänd

---

## 1. Systemöversikt

DartMaster är en webbaserad plattform för hantering av dartturnering med live-uppdateringar, matchrapportering och detaljerad statistik. Systemet utvecklas med:

- **Frontend:** React JS (webb), React Native (mobil, framtida)
- **Backend:** C# Minimal API med JWT-autentisering
- **Database:** MariaDB
- **Hosting:** Kubernetes-kluster
- **Språk:** Svenska och Engelska

---

## 2. Användarroller & Autentisering

### 2.1 Användarroller
- **Admin**: Kan skapa/redigera turnering, ändra resultat, hantera användare
- **Spelare**: Kan registrera sig, delta i turnering, rapportera matchresultat
- **Åskådare**: Icke-aktiva spelare som kan se live-uppdateringar och slutresultat

### 2.2 Autentisering
- Använd JWT-tokens för säker autentisering
- Registreringsprocess för nya spelare (self-service)
- Password reset via email
- HTTPS obligatoriskt på all kommunikation

---

## 3. Turneringar

### 3.1 Turneringsformat (MVP: Grupper)
- **Gruppspel**: Spelare delas i grupper, spelar alla mot alla inom gruppen
- **Seriespel**: Löpande matcher mellan spelare (framtid)
- **Knockout**: Eliminationsformat (framtid)

### 3.2 Turneringskonfiguration
- Admin kan skapa ny turnering med:
  - Namn och beskrivning
  - Startdatum och tid
  - Antal grupper/spelare
  - Matchformat (301 eller 501)
  - Registreringsdeadline
- Stöd för upp till 100 spelare per turnering
- Flera turnering kan köra samtidigt
- Admin kan ändra turneringsinställningar innan start

### 3.3 Deltagare
- Spelare kan anmäla sig själva
- Admin kan göra manuell anmälan
- Admin kan markera spelare som frånvarande (WO - Walk Over)
- Ingen väntelista (full är full)

---

## 4. Matcher

### 4.1 Matchformat
**MVP:**
- **301**: Spelare börjar med 301 poäng och måste få exakt 0 (med dubbel)

**Framtid:**
- **501**: Spelare börjar med 501 poäng

### 4.2 Matchdeltagare
- En match kan ha **1-6 spelare** samtidigt
- Gruppformat: Alla spelare i gruppen spelar alla mot alla (round-robin)

### 4.3 Resultatrapportering
- **En spelare** rapporterar resultat för hela matchen
- **Andra spelare** i matchen måste bekräfta resultatet
- Tillsammans måste minst 50% bekräfta innan resultat är slutgiltigt
- Admin kan ändra/korrigera resultat efter rapportering

### 4.4 Pilkast-registrering
- Systemet registrerar **varje pilkast** under matchens gång
- Spela kan manuellt mata in poäng per pilkast
- Systemet **räknar automatiskt** återstående poäng
- Systemet validerar: kan inte sluta på <0 poäng, måste sluta på dubbel

### 4.5 Avbrutna matcher
- Admin kan markera match som **WO (Walk Over)** om nödvändigt
- Motspelarnas poäng registreras som en vinst

---

## 5. Live-uppdateringar

### 5.1 Tekniska krav
- **Realtidsuppdateringar** utan att sidan behöver uppdateras manuellt
- WebSocket eller liknande för push-data från server
- Åskådare ser:
  - Pågående match-poäng (live scoring)
  - Slutresultat
  - Turneringstabeller uppdateras i realtid

### 5.2 Prestanda
- Stöd för många samtidiga matcher och åskådare
- Optimerad för ~100 spelare + åskådare

---

## 6. Statistik & Ranking

### 6.1 Spelstatistik
- Vunna matcher
- Förlorade matcher
- Win/Loss-ratio
- Genomsnittlig poäng per match
- Head-to-head resultat mellan spelare
- Turneringsbhistorik (tidigare resultat)

### 6.2 Ranking
- System ska beräkna ranking baserat på:
  - Prestation i tidigare turnering
  - Antal vunna matcher
  - Poänggenomsnitt

---

## 7. Notifikationer

### 7.1 Push-notifikationer
- **Match-resultat rapporterat**: Alla deltagare i matchen
- Implementeras via webpush eller app-notifikationer

### 7.2 Email-notifikationer
- **Match-resultat rapporterat**: Alla deltagare
- **Turnering börjar snart**: Anmälda spelare
- **Din nästa match**: Innan match startar
- **Framtid**: Mer granulär konfiguration

---

## 8. Admin-funktioner

### 8.1 Turneringsöversikt
- Se status på alla turnering
- Skapa ny turnering
- Redigera turneringsinställningar

### 8.2 Matchhantering
- Se alla matcher i turnering
- Ändra/korrigera matchresultat
- Markera matcher som avbrutna (WO)
- Hantera resultatbekräftelser

### 8.3 Användarhantering
- Se alla spelare
- Aktivera/inaktivera spelare
- Redigera spelarinfo
- Reset password

### 8.4 Rapporter
- Turneringhistorik
- Spelstatistik
- Resultathistorik

---

## 9. Datakrav

### 9.1 Användardata
```
- User ID (UUID)
- Användarnamn (unikt)
- Email (unikt)
- Lösenord (hashed)
- Fullständigt namn
- Roll (Admin/Spelare/Åskådare)
- Skapningsdatum
- Senaste inloggning
```

### 9.2 Turneringdata
```
- Tournament ID (UUID)
- Namn
- Beskrivning
- Status (Planning, Active, Completed)
- Startdatum
- Slutdatum
- Format (Group, Series, Knockout)
- Matchformat (301, 501)
- Admin (User ID)
- Grupper (Array)
- Deltagare (Array)
```

### 9.3 Matchdata
```
- Match ID (UUID)
- Tournament ID
- Group ID (om gruppformat)
- Deltagare (Array av User IDs)
- Format (301/501)
- Status (Scheduled, Live, Waiting for confirmation, Completed)
- Starttid
- Sluttid
- Resultat (Array av pilkast)
  - Pilkast ID
  - Spelare ID
  - Poäng
  - Tidsstämpel
- Slutresultat (vinnar, order)
- Bekräftelser (Array)
```

### 9.4 Statistikdata
```
- User ID
- Tournament ID
- Matchade spelade
- Matchade vunna
- Win/Loss ratio
- Genomsnittlig poäng
- Ranking
```

---

## 10. API-Endpoints (C# Minimal API)

### 10.1 Autentisering
- `POST /api/auth/register` - Ny användare
- `POST /api/auth/login` - Inloggning
- `POST /api/auth/refresh` - JWT-refresh
- `POST /api/auth/logout` - Utloggning
- `POST /api/auth/forgot-password` - Lösenordsåterställning

### 10.2 Turnering
- `GET /api/tournaments` - Lista alla
- `GET /api/tournaments/{id}` - Detaljer
- `POST /api/tournaments` - Skapa (admin)
- `PUT /api/tournaments/{id}` - Uppdatera (admin)
- `DELETE /api/tournaments/{id}` - Radera (admin)

### 10.3 Matcher
- `GET /api/matches` - Lista matcher
- `GET /api/matches/{id}` - Matchdetaljer
- `POST /api/matches/{id}/score` - Rapportera pilkast
- `POST /api/matches/{id}/confirm` - Bekräfta resultat
- `PUT /api/matches/{id}` - Uppdatera (admin)

### 10.4 Användare
- `GET /api/users/{id}` - Profilinfo
- `GET /api/users/{id}/stats` - Statistik
- `PUT /api/users/{id}` - Uppdatera profil
- `GET /api/users/{id}/history` - Matchhistorik

### 10.5 Admin
- `GET /api/admin/users` - Alla användare
- `GET /api/admin/tournaments` - Alla turnering
- `PUT /api/admin/users/{id}` - Redigera användare
- `PUT /api/admin/matches/{id}` - Redigera match

---

## 11. UI/UX-komponenter

### 11.1 Publika sidor
- Landningsida
- Registreringssida
- Inloggningssida
- Turneringslista
- Turnering-detaljer (live scoreboard)

### 11.2 Spelar-dashboard
- Mina turnering
- Mina matchresultat
- Min statistik/ranking
- Mina notifikationer

### 11.3 Admin-dashboard
- Turneringssöversikt
- Matchhantering
- Resultatbekräftelser
- Användarhantering

### 11.4 Match-interface
- Live-poängregistrering
- Pilkast-input
- Poäng-validering
- Resultat-sammanfattning

---

## 12. Säkerhet

### 12.1 Autentisering & Auktorisering
- JWT-tokens med 1-timmes expiry
- Refresh-tokens med längre expiry
- Role-based access control (RBAC)
- Secure password hashing (bcrypt)

### 12.2 Data-säkerhet
- HTTPS/TLS obligatoriskt
- Input-validering på server-sida
- SQL Injection-skydd (prepared statements)
- CSRF-skydd

### 12.3 API-säkerhet
- Rate limiting
- Validering av alla inputs
- Logging av admin-åtgärder

---

## 13. MVP-scope (Launch v1.0)

**Inkluderat:**
- ✅ Användarregistrering och inloggning
- ✅ Skapa turnering (gruppformat)
- ✅ 301-format
- ✅ Pilkast-registrering (manuell input)
- ✅ Resultatrapportering och bekräftelse
- ✅ Live-scoreboard för åskådare
- ✅ Grundläggande statistik
- ✅ Push- och email-notifikationer (match-resultat)
- ✅ Admin-dashboard för resultat-ändring
- ✅ Svenska och engelska

**Framtida (v2.0+):**
- ❌ 501-format
- ❌ Knockout-format
- ❌ Seriespel
- ❌ Mobilapp (React Native)
- ❌ Avancerad statistik/ranking
- ❌ Integrationer
- ❌ Vän-system
- ❌ Lagformat

---

## 14. Teknisk Stack

| Komponent | Val |
|-----------|-----|
| Backend | C# Minimal API (.NET 8) |
| Frontend | React JS (TypeScript) |
| Database | MariaDB |
| Auth | JWT + Bearer tokens |
| Realtid | WebSocket (SignalR) |
| Hosting | Kubernetes |
| CI/CD | (Att definieras) |
| Logging | (Att definieras) |

---

## 15. Nästa steg

1. ✅ **Kravspecifikation godkänd** (denna fil)
2. **Arkitektur-design**: Backend API-struktur, databas-schema
3. **Databas-schema**: Skapa tabell-design
4. **Backend setup**: C# Minimal API projekt
5. **Frontend setup**: React-projekt
6. **Iterativ utveckling**: Feature by feature

---

## Dokument-historik

| Version | Datum | Ändringar |
|---------|-------|-----------|
| 1.0 | 2026-02-11 | Första version, baserat på initial intervju |

---

**Godkänd av:** Mattias Revelj  
**Datum:** 2026-02-11
