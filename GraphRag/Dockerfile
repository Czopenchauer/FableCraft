FROM python:3.12-slim

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    curl \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .

RUN pip install --no-cache-dir -r requirements.txt

COPY . .

RUN mkdir -p /app/cognee/data /app/cognee/system /app/visualization

ENV PYTHONUNBUFFERED=1
ENV PORT=8111

EXPOSE 8111

CMD ["uvicorn", "api:app", "--host", "0.0.0.0", "--port", "8111"]
