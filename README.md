# ClaimLens

[![CI](https://github.com/Syartaa/ClaimLens/actions/workflows/ci.yml/badge.svg)](https://github.com/Syartaa/ClaimLens/actions/workflows/ci.yml)

**Investigate claims. Compare evidence. Understand the context.**

ClaimLens is an evidence-first investigation workspace. A person submits a claim or a piece of article text. The system extracts the factual claims, gathers evidence, and classifies each piece as supporting, contradicting, or neutral. A deterministic calculator in the backend then produces a verdict and a system confidence score. The claims, sources, and reasoning stay on the page.

The language model does not decide whether a claim is true. It extracts, structures, classifies, and writes the summary from the calculator’s result. When the evidence is too thin, the verdict is **Insufficient Evidence**. System confidence measures how strongly this system’s stored evidence supports that verdict. It is not the probability that the claim is true.

A reviewer can confirm or override a classification. The original model output stays stored. An administrator can see users, failed runs, and which evidence provider was used. In the current build, evidence comes from labeled development fixtures, and the page says so.

ClaimLens is built with React, ASP.NET Core, FastAPI, and PostgreSQL.
