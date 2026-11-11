# ClaimLens AI service

FastAPI health check only. Claim extraction starts in a later phase.

```bash
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
pytest
ruff check .
```

Health: `GET http://localhost:8000/health`.
