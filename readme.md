markdown
# 🛒 Containerized E-Commerce Microservices System

## 📌 Overview

This project implements a containerized e-commerce system using a microservices architecture.

The system demonstrates:

- API Gateway pattern
- Aggregation pattern
- Database per service
- Synchronous HTTP communication
- Asynchronous event-driven messaging with RabbitMQ
- Docker-based container orchestration

---

## 🏗️ Architecture

### Services

| Service | Port |
|----------|-------|
| API Gateway (Ocelot) | 5050 |
| Order Service (Aggregation Logic) | 5002 |
| Customer Service | 5001 |
| Product Service | 5003 |
| Payment Service | 5004 |
| RabbitMQ | 5672 |

### Communication

**Synchronous (HTTP):**
- Client → API Gateway
- API Gateway → Order Service
- Order Service → Customer Service
- Order Service → Product Service

**Asynchronous (Event-Driven):**
- Order Service publishes:
  - `OrderCreated`
  - `OrderCancelled`
- Payment Service consumes events
- Product Service consumes events

---

## 🐳 Run with Docker

```bash
docker-compose up --build

After startup:

API Gateway → http://localhost:5050
RabbitMQ → http://localhost:15672
username: guest
password: guest
🧱 Technologies
.NET
Ocelot API Gateway
RabbitMQ
Docker & Docker Compose
REST APIs
🎯 Purpose
This project demonstrates the design and implementation of a distributed microservices system with both synchronous and event-driven communication patterns.

Author: Zhongqiang Yu | 2084935
Course: CSCI 6844 | Programming for the Internet