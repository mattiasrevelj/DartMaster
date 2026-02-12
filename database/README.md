# DartMaster Database Setup

## MariaDB Setup for Development

### Prerequisites
- Docker Desktop (installed and running)
- Docker Compose

### Getting Started

#### 1. Start the Database
```bash
cd c:\Dev\DartMaster
docker-compose up -d
```

This will start:
- **MariaDB** on `localhost:3306`
- **phpMyAdmin** on `http://localhost:8080`

#### 2. Connect to the Database

**Connection Details:**
- **Host:** localhost
- **Port:** 3306
- **Username:** dartmaster
- **Password:** dartmaster_pass
- **Database:** dartmaster

**From Command Line:**
```bash
# Using mysql client
mysql -h localhost -u dartmaster -p dartmaster -D dartmaster

# Or with mariadb client
mariadb -h localhost -u dartmaster -p dartmaster -D dartmaster
```

**From Your C# Application:**
```csharp
var connectionString = "Server=localhost;Port=3306;Database=dartmaster;User Id=dartmaster;Password=dartmaster_pass;";
```

#### 3. Access phpMyAdmin
- URL: http://localhost:8080
- Username: dartmaster
- Password: dartmaster_pass
- Server: mariadb

### Database Schema

The database automatically initializes with the schema from `database/schema.sql` on first startup.

**Tables created:**
- `users` - User accounts and authentication
- `tournaments` - Tournament information
- `tournament_groups` - Groups within tournaments
- `tournament_participants` - Participants in tournaments
- `matches` - Matches in tournament
- `match_participants` - Players in specific match
- `dart_throws` - Individual dart throws
- `match_confirmations` - Result confirmations
- `player_statistics` - Player stats per tournament
- `notifications` - User notifications
- `notification_subscriptions` - Push/email subscriptions
- `refresh_tokens` - JWT refresh tokens

### Database Credentials

**Root User:**
- Username: `root`
- Password: `rootpassword`

**Application User:**
- Username: `dartmaster`
- Password: `dartmaster_pass`
- Database: `dartmaster`

### Commands

```bash
# View logs
docker-compose logs -f mariadb

# Stop containers
docker-compose down

# Stop and remove all data
docker-compose down -v

# Restart containers
docker-compose restart

# Execute SQL command
docker exec dartmaster-db mariadb -u dartmaster -p dartmaster_pass dartmaster -e "SHOW TABLES;"

# Backup database
docker exec dartmaster-db mysqldump -u dartmaster -p dartmaster_pass dartmaster > backup.sql

# Restore database
docker exec -i dartmaster-db mariadb -u dartmaster -p dartmaster_pass dartmaster < backup.sql
```

### Troubleshooting

**Port 3306 already in use:**
Edit `docker-compose.yml` and change:
```yaml
ports:
  - "3307:3306"  # Use 3307 instead of 3306
```

**Cannot connect to database:**
1. Check if containers are running: `docker ps`
2. Check logs: `docker-compose logs mariadb`
3. Wait a few seconds for MariaDB to fully initialize

**Permission denied errors:**
Ensure you're running with proper Docker permissions or use `sudo` on Linux.

### Development Workflow

1. **Initialize:** `docker-compose up -d`
2. **Check status:** `docker-compose ps`
3. **View logs:** `docker-compose logs -f`
4. **Clean up:** `docker-compose down`

### Production Notes

For production, update `docker-compose.yml`:
- Change `rootpassword` and `dartmaster_pass` to strong passwords
- Use environment variables from `.env` file
- Configure persistent volumes properly
- Add backup policies
- Add monitoring/health checks

---

**Last Updated:** 2026-02-12
