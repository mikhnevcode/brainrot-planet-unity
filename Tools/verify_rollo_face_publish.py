"""v0.8.4 public artifact verification, separate from the v0.8.3 snapshot."""
import verify_rollo_sculpt_publish as Q
Q.REPORT='QA/v084/rollo-face-report.json';Q.PAGE='test-v0.8.4.html';Q.NAMESPACE='rollo084'
Q.CATALOG='catalog-original26-v6.json';Q.CATALOG_REVISION=7
Q.OUTPUT='QA/v084/publish-verification.json';Q.LABEL='ROLLO_FACE'
Q.HEADERS={'User-Agent':'BrainrotPlanet-QA/0.8.4'}
if __name__=='__main__':Q.main()
