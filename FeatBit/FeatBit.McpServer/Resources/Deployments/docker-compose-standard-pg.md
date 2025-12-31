---
name: docker-compose-standard-postgres
description: Deploy FeatBit using Docker Compose with PostgreSQL and Redis. This Solution provides a more performance-optimized setup compared to standalone deployments.
---

# FeatBit Docker Compose Deployment (Standard with PostgreSQL)

## Quick deployment

Run the standard Docker Compose deployment with PostgreSQL:

```bash
git clone https://github.com/featbit/featbit
cd featbit
docker compose -f docker-compose-standard.yml up -d
```

Access the portal at [http://localhost:8081](http://localhost:8081)

**Default credentials:**
- Username: `test@featbit.com`
- Password: `123456`

## Complete Docker Compose configuration

Save this as `docker-compose-standard.yml`:

```yaml
name: featbit
services:
  ui:
    image: featbit/featbit-ui:latest
    environment:
      - API_URL=http://localhost:5000
      - DEMO_URL=https://featbit-samples.vercel.app
      - EVALUATION_URL=http://localhost:5100
      - BASE_HREF=/
    depends_on:
      - api-server
    ports:
      - "8081:80"
    networks:
      - featbit-network

  api-server:
    image: featbit/featbit-api-server:latest
    environment:
      - DbProvider=Postgres
      - MqProvider=Redis
      - CacheProvider=Redis
      - Postgres__ConnectionString=Host=postgresql;Port=5432;Username=postgres;Password=please_change_me;Database=featbit
      - Redis__ConnectionString=redis:6379
      - OLAP__ServiceHost=http://da-server
    depends_on:
      - postgresql
      - redis
      - da-server
    ports:
      - "5000:5000"
    networks:
      - featbit-network

  evaluation-server:
    image: featbit/featbit-evaluation-server:latest
    environment:
      - DbProvider=Postgres
      - MqProvider=Redis
      - CacheProvider=Redis
      - Postgres__ConnectionString=Host=postgresql;Port=5432;Username=postgres;Password=please_change_me;Database=featbit
      - Redis__ConnectionString=redis:6379
    depends_on:
      - postgresql
      - redis
    ports:
      - "5100:5100"
    networks:
      - featbit-network

  da-server:
    image: featbit/featbit-data-analytics-server:latest
    depends_on:
      - postgresql
    ports:
      - "8200:80"
    networks:
      - featbit-network
    environment:
      DB_PROVIDER: Postgres
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: please_change_me
      POSTGRES_HOST: postgresql
      POSTGRES_PORT: 5432
      POSTGRES_DATABASE: featbit
      CHECK_DB_LIVNESS: true

  postgresql:
    image: postgres:15.10
    container_name: postgresql
    restart: on-failure
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: please_change_me
    volumes:
      - ./infra/postgresql/docker-entrypoint-initdb.d/:/docker-entrypoint-initdb.d/
      - postgres:/var/lib/postgresql/data
    networks:
      - featbit-network
  
  redis:
    image: bitnamilegacy/redis:6.2.10
    container_name: redis
    restart: on-failure
    environment:
      - ALLOW_EMPTY_PASSWORD=yes
    networks:
      - featbit-network
    ports:
      - "6379:6379"
    volumes:
      - redis:/bitnami/redis/data

networks:
  featbit-network:
    name: featbit-network
    driver: bridge
    ipam:
      config:
        - subnet: 172.10.0.0/16

volumes:
  postgres:
  redis:
```

## Architecture overview

The standard deployment includes five services:

| Service | Port | Purpose |
|---------|------|---------|
| **ui** | 8081 | Angular portal for feature flag management |
| **api-server** | 5000 | .NET API server for management operations |
| **evaluation-server** | 5100 | .NET evaluation server for flag evaluation |
| **da-server** | 8200 | Data analytics server for insights |
| **postgresql** | 5432 | PostgreSQL database for persistent storage |
| **redis** | 6379 | Redis for caching and message queue |

## Production considerations

**MUST change before production:**

1. **Update database password:**
   ```yaml
   # Change 'please_change_me' in all connection strings
   - Postgres__ConnectionString=Host=postgresql;Port=5432;Username=postgres;Password=YOUR_SECURE_PASSWORD;Database=featbit
   ```

2. **Set Redis authentication:**
   ```yaml
   # Replace ALLOW_EMPTY_PASSWORD with proper auth
   environment:
     - REDIS_PASSWORD=your_redis_password
   ```

3. **Configure external access:**
   - Update `API_URL` and `EVALUATION_URL` in UI config
   - Configure reverse proxy (nginx/traefik) for HTTPS
   - Set proper network policies

4. **Enable persistence:**
   - Named volumes are configured by default
   - Verify mount paths: `postgres:/var/lib/postgresql/data`, `redis:/bitnami/redis/data`

## Troubleshooting

### Services won't start
1. Check port availability: `netstat -an | grep "8081\|5000\|5100\|5432\|6379"`
2. Verify Docker daemon: `docker info`
3. Check service health: `docker compose -f docker-compose-standard.yml ps`

### Cannot access portal
1. Verify UI container: `docker logs featbit-ui-1`
2. Check `API_URL` matches your host configuration
3. For external access, see [FAQ](https://docs.featbit.co/installation/faq#how-to-make-featbit-portal-accessible-publicly)

### Database connection errors
1. Verify PostgreSQL is running: `docker compose -f docker-compose-standard.yml logs postgresql`
2. Test connection: `docker exec -it postgresql psql -U postgres -d featbit -c "SELECT 1;"`
3. Check initialization scripts ran: `docker compose -f docker-compose-standard.yml logs postgresql | grep "init"`

### Redis connection errors
1. Check Redis container: `docker compose -f docker-compose-standard.yml logs redis`
2. Test connection: `docker exec -it redis redis-cli ping`