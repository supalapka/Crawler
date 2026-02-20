# Cryptocurrency News Crawler

Background worker for crawling and parsing cryptocurrency news.
Integrated with the Cryptocurrency Exchange back-end via RabbitMQ.

Back-end:
- https://github.com/supalapka/CryptocurrencyExchange-Backend

## Requirements
- .NET 6
- RabbitMQ

## Notes
- Runs as a background service
- Publishes parsed news events via RabbitMQ
