from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_health() -> None:
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "ok", "service": "claimlens-ai"}


def test_extract_claim_returns_one_structured_claim() -> None:
    response = client.post(
        "/v1/claims/extract",
        json={"text": "Drinking coffee causes dehydration.", "inputType": "Claim"},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["model"] == "mock"
    assert body["claims"] == [
        {"text": "Drinking coffee causes dehydration.", "type": "factual", "importance": 0.9}
    ]


def test_classify_matching_excerpt_is_supporting() -> None:
    response = client.post(
        "/v1/evidence/classify",
        json={
            "claimText": "Vitamin C prevents the common cold.",
            "excerpt": "Vitamin C prevents the common cold.",
        },
    )
    assert response.status_code == 200
    body = response.json()
    assert body["model"] == "mock"
    assert body["stance"] == "supporting"
    assert 0 <= body["relevance"] <= 1
    assert 0 <= body["confidence"] <= 1


def test_summary_quotes_the_calculator_verdict() -> None:
    response = client.post(
        "/v1/summary",
        json={"verdict": "Mixed", "confidence": 0.62, "explanation": "Support and contradiction are similar."},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["model"] == "mock"
    assert "Mixed" in body["summary"]
    assert "not the probability" in body["summary"]


def test_extract_rejects_empty_text() -> None:
    response = client.post("/v1/claims/extract", json={"text": "", "inputType": "Claim"})
    assert response.status_code == 422
