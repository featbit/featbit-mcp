---
name: docker-compose-standalone
description: Deploys FeatBit Standalone with Docker Compose using only PostgreSQL. Use when the user asks about Docker Compose deployment, standalone deployment, minimal FeatBit setup, or PostgreSQL-only deployment.
---

# FeatBit Standalone Deployment

FeatBit Standalone is the minimal deployment: all core services with PostgreSQL only (no MongoDB, no Redis).

## Quick Start

Create `docker-compose.yml`:

## Quick Start

Create `docker-compose.yml`:

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
      - MqProvider=Postgres
      - CacheProvider=None
      - Postgres__ConnectionString=Host=postgresql;Port=5432;Username=postgres;Password=please_change_me;Database=featbit
      - OLAP__ServiceHost=http://da-server
    depends_on:
      - postgresql
      - da-server
    ports:
      - "5000:5000"
    networks:
      - featbit-network

  evaluation-server:
    image: featbit/featbit-evaluation-server:latest
    environment:
      - DbProvider=Postgres
      - MqProvider=Postgres
      - CacheProvider=None
      - Postgres__ConnectionString=Host=postgresql;Port=5432;Username=postgres;Password=please_change_me;Database=featbit
    depends_on:
      - postgresql
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

networks:
  featbit-network:
    name: featbit-network
    driver: bridge
    ipam:
      config:
        - subnet: 172.10.0.0/16

volumes:
  postgres:
```

Deploy:

```bash
docker-compose up -d
```

Access:
- UI: http://localhost:8081 (login: test@featbit.com / 123456)
- API: http://localhost:5000
- Evaluation Server: http://localhost:5100
- Data Analytics: http://localhost:8200

⚠️ Change default password immediately.

## Key Configuration

**PostgreSQL-Only Architecture:**
- `DbProvider=Postgres` - data storage
- `MqProvider=Postgres` - message queue via PostgreSQL
- `CacheProvider=None` - no external cache

**Security:**
Change `POSTGRES_PASSWORD` in all services before production use.

**Data Persistence:**
PostgreSQL data persists in Docker volume `postgres`. Place initialization scripts in `./infra/postgresql/docker-entrypoint-initdb.d/`.

## Services

- **ui** (8081) - Web interface
- **api-server** (5000) - REST API
- **evaluation-server** (5100) - Feature flag evaluation
- **da-server** (8200) - Analytics processing
- **postgresql** (5432) - Database

## Prerequisites

- Docker Engine 20.10+
- Docker Compose V2 (2.0+)
