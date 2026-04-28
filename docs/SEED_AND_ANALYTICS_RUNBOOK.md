# Seed and Analytics Runbook

## 1) Hybrid seed (1000+ rows)

### Endpoint (Admin token required)

`POST /api/admin/seed/hybrid`

Example:

```bash
curl -X POST "https://localhost:5001/api/admin/seed/hybrid" \
  -H "Authorization: Bearer <ADMIN_JWT>"
```

Expected response shape:

```json
{
  "sportEventsAdded": 1000,
  "usersAdded": 100,
  "betsAdded": 1500,
  "betLegsAdded": 3000,
  "transactionsAdded": 1600
}
```

## 2) Verify counts in DB

Run SQL checks:

```sql
select count(*) as sport_events from "SportEvents";
select count(*) as users_count from "Users";
select count(*) as bets_count from "Bets";
select count(*) as bet_legs_count from "BetLegs";
select count(*) as transactions_count from "Transactions";
```

## 3) User analytics API (date range)

Endpoint:

`GET /api/bet/analytics/mine?from=<ISO_UTC>&to=<ISO_UTC>`

Example:

```bash
curl "https://localhost:5001/api/bet/analytics/mine?from=2026-01-01T00:00:00Z&to=2026-03-31T23:59:59Z" \
  -H "Authorization: Bearer <USER_JWT>"
```

Response includes:

- totalBets
- totalStake
- totalPayout
- net
- winRatePercent
- points[] grouped by day

## 4) Frontend demo path

1. Sign in as regular user.
2. Open `History`.
3. Set `From`/`To` filters.
4. Click `Apply`.
5. Show KPI cards and daily chart bars.
