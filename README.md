# Cryptocurrency News Crawler

Background worker for crawling and parsing cryptocurrency news.
Integrated with the Cryptocurrency Exchange back-end via RabbitMQ.

Back-end:
- https://github.com/supalapka/CryptocurrencyExchange-Backend

## Requirements
- .NET 6
- RabbitMQ with `rabbitmq_delayed_message_exchange` plugin

## Running with Docker

**1. Start RabbitMQ:**
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 heidiks/rabbitmq-delayed-message-exchange:latest
```

**2. Build the worker(crawler) image:**
```bash
docker build -f Dockerfile.worker -t crawler-worker .
```

**3. Run the worker:**
```bash
docker run --rm \
  -e RabbitMq__Username=youruser \
  -e RabbitMq__Password=yourpass \
  crawler-worker
```
Or run and set ENV for RabbitMq__Username and RabbitMq_Password in another way

---

> `RabbitMq__Host` defaults to `host.docker.internal` and does not need to be set if RabbitMQ is running on the host machine.

## Notes
- Runs as a background service
- Publishes parsed news events via RabbitMQ
