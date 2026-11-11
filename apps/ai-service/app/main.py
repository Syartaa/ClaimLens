import re

from fastapi import FastAPI, Header, HTTPException
from pydantic import BaseModel, Field

app = FastAPI(title="ClaimLens AI", version="0.1.0")

ALLOWED_TYPES = {"factual", "statistical", "scientific", "historical", "medical", "political", "other"}


class ExtractRequest(BaseModel):
    text: str = Field(min_length=1, max_length=50000)
    inputType: str


class ClaimOut(BaseModel):
    text: str
    type: str
    importance: float = Field(ge=0, le=1)


class ExtractResponse(BaseModel):
    claims: list[ClaimOut]
    promptName: str
    promptVersion: str
    model: str


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "claimlens-ai"}


@app.post("/v1/claims/extract", response_model=ExtractResponse)
def extract_claims(body: ExtractRequest, x_internal_key: str | None = Header(default=None)) -> ExtractResponse:
    _require_key(x_internal_key)
    claims = _mock_claims(body.text, body.inputType)
    return ExtractResponse(
        claims=claims,
        promptName="claim-extraction",
        promptVersion="v1",
        model="mock",
    )


class ClassifyRequest(BaseModel):
    claimText: str = Field(min_length=1, max_length=50000)
    excerpt: str = Field(min_length=1, max_length=50000)


class ClassifyResponse(BaseModel):
    stance: str
    relevance: float = Field(ge=0, le=1)
    confidence: float = Field(ge=0, le=1)
    explanation: str
    promptName: str
    promptVersion: str
    model: str


@app.post("/v1/evidence/classify", response_model=ClassifyResponse)
def classify_evidence(body: ClassifyRequest, x_internal_key: str | None = Header(default=None)) -> ClassifyResponse:
    _require_key(x_internal_key)
    stance, relevance, confidence = _mock_stance(body.claimText, body.excerpt)
    return ClassifyResponse(
        stance=stance,
        relevance=relevance,
        confidence=confidence,
        explanation="Mock classification from the excerpt text. This is not a verdict.",
        promptName="evidence-classification",
        promptVersion="v1",
        model="mock",
    )


def _mock_stance(claim_text: str, excerpt: str) -> tuple[str, float, float]:
    claim = " ".join(claim_text.split()).lower()
    text = " ".join(excerpt.split()).lower()
    if text == claim:
        return "supporting", 0.9, 0.8
    if "does not" in text or "do not" in text or "did not" in text:
        return "contradicting", 0.9, 0.8
    return "neutral", 0.45, 0.5


class SummaryRequest(BaseModel):
    verdict: str = Field(min_length=1, max_length=64)
    confidence: float = Field(ge=0, le=1)
    explanation: str = Field(min_length=1, max_length=20000)


class SummaryResponse(BaseModel):
    summary: str
    model: str


@app.post("/v1/summary", response_model=SummaryResponse)
def summarize(body: SummaryRequest, x_internal_key: str | None = Header(default=None)) -> SummaryResponse:
    _require_key(x_internal_key)
    return SummaryResponse(
        summary=(
            f"Mock summary. The calculator returned {body.verdict} "
            f"with system confidence {body.confidence:.2f}. "
            "That score is not the probability the claim is true."
        ),
        model="mock",
    )


def _require_key(provided: str | None) -> None:
    import os

    expected = os.environ.get("AI_SERVICE_KEY", "")
    if expected and provided != expected:
        raise HTTPException(status_code=401, detail="Invalid internal key")


def _mock_claims(text: str, input_type: str) -> list[ClaimOut]:
    cleaned = " ".join(text.split())
    if input_type == "Claim":
        return [ClaimOut(text=cleaned, type="factual", importance=0.9)]

    sentences = [part.strip() for part in re.split(r"(?<=[.!?])\s+", cleaned) if part.strip()]
    selected = sentences[:5] or [cleaned]
    return [
        ClaimOut(text=sentence, type="factual", importance=round(max(0.4, 0.9 - (index * 0.1)), 2))
        for index, sentence in enumerate(selected)
    ]
