export const locales = ["pt-BR", "en"] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = "pt-BR";
export function isLocale(value: string): value is Locale {
  return locales.includes(value as Locale);
}

const en = {
  brand: "Research intelligence",
  desk: "THE RESEARCH DESK",
  connected: "CONNECTED BY EVIDENCE",
  readingChain: "CLAIM → EVIDENCE → SOURCE",
  contextIndex: "CONTEXT",
  evidenceIndex: "EVIDENCE",
  limitationsIndex: "LIMITATIONS",
  sourcesIndex: "SOURCES",
  recordIndex: "RECORD",
  canonicalNote:
    "Canonical claims and quoted evidence are preserved in their source language.",
  brandSub: "An evidence-first view of AI",
  research: "Research feed",
  methodology: "Our approach",
  rss: "RSS feed",
  preview: "Foundation preview",
  previewNote:
    "Demo publications are synthetic fixtures, not real research or verified findings.",
  synthetic: "Synthetic example",
  syntheticDetail:
    "This is a synthetic demonstration. Its claims, evidence, sources and publication history are fictional test data. Do not use it to make research or business decisions.",
  eyebrow: "RESEARCH, WITH CONTEXT",
  heroTitle: "Beyond the headline.",
  heroAccent: "Back to the evidence.",
  heroDescription:
    "Explore AI developments through the claims, sources and limitations behind them. One research record, a clearer perspective.",
  browse: "Explore the research",
  latest: "Latest research",
  latestNote: "Published records, with their evidence in view.",
  publication: "publication",
  publications: "publications",
  all: "All topics",
  search: "Search titles, summaries or topics",
  searchLabel: "Search research",
  reset: "Clear filters",
  noResults: "No matching research",
  noResultsNote: "Try another topic or a broader search.",
  empty: "No publications yet",
  emptyNote:
    "Published research will appear here once it has passed editorial review.",
  read: "Read the analysis",
  evidence: "Evidence strength",
  version: "Version",
  published: "Published",
  back: "Back to research",
  why: "Why it matters",
  claims: "Claims & evidence",
  claimsNote:
    "What is claimed, what supports it, and where that support comes from.",
  claim: "Claim",
  confidence: "Confidence",
  supportingEvidence: "Supporting evidence",
  noEvidence: "No supporting evidence is attached to this claim.",
  limitations: "Limitations & open questions",
  noLimitations:
    "No limitations were supplied for this publication. This does not establish that the findings generalize.",
  sources: "Primary sources",
  source: "Source",
  sourceDate: "Source published",
  openSource: "Open source",
  history: "Publication history",
  historyNote:
    "Changes remain visible. This page shows the latest published version.",
  record: "Research record",
  onThisPage: "In this analysis",
  topics: "Topics",
  approachTitle: "Clarity is a research practice.",
  approachDescription:
    "A useful headline is a starting point. We keep the reasoning behind it visible, so you can judge what the evidence actually supports.",
  approachOne: "Separate the claim",
  approachOneText:
    "Distinguish what authors say from what has been independently demonstrated.",
  approachTwo: "Trace the evidence",
  approachTwoText:
    "Follow each claim to its source, with excerpts and precise locations.",
  approachThree: "Keep the caveats",
  approachThreeText:
    "Read the limitations alongside the result, and see how the record changes.",
  asideTitle: "A clearer reading path",
  asideText: "Follow the chain from a finding to the evidence behind it.",
  asideClaim: "The claim",
  asideClaimText: "What is being asserted?",
  asideEvidence: "The evidence",
  asideEvidenceText: "What supports it?",
  asideSource: "The source",
  asideSourceText: "Where can you verify it?",
  bilingual: "Two languages. One research record.",
  bilingualText:
    "English and Brazilian Portuguese share the same canonical claims, citations and publication versions.",
  footerNote: "Context before conclusions. Evidence before certainty.",
  status: "Public preview · Synthetic demo data",
  unavailable: "Research is temporarily unavailable",
  unavailableNote:
    "We couldn’t reach the research service. Please try again in a moment.",
  retry: "Try again",
  notFound: "This research page isn’t available",
  notFoundNote:
    "It may not have been published in this language, or the address may be incorrect.",
  notFoundGlobal: "Page not found",
  loading: "Loading research…",
} as const;

const pt: { [K in keyof typeof en]: string } = {
  brand: "Inteligência de pesquisa",
  desk: "MESA DE PESQUISA",
  connected: "CONECTADAS POR EVIDÊNCIAS",
  readingChain: "ALEGAÇÃO → EVIDÊNCIA → FONTE",
  contextIndex: "CONTEXTO",
  evidenceIndex: "EVIDÊNCIAS",
  limitationsIndex: "LIMITAÇÕES",
  sourcesIndex: "FONTES",
  recordIndex: "REGISTRO",
  canonicalNote:
    "As alegações canônicas e os trechos citados são preservados no idioma de origem.",
  brandSub: "Uma visão de IA guiada por evidências",
  research: "Pesquisas",
  methodology: "Nossa abordagem",
  rss: "Feed RSS",
  preview: "Prévia da plataforma",
  previewNote:
    "As publicações de demonstração são fictícias, não pesquisas reais ou resultados verificados.",
  synthetic: "Exemplo sintético",
  syntheticDetail:
    "Esta é uma demonstração sintética. Suas alegações, evidências, fontes e versões são dados fictícios de teste. Não use este conteúdo para tomar decisões de pesquisa ou de negócios.",
  eyebrow: "PESQUISA, COM CONTEXTO",
  heroTitle: "Além da manchete.",
  heroAccent: "De volta às evidências.",
  heroDescription:
    "Explore os avanços em IA pelas alegações, fontes e limitações que os sustentam. Um registro de pesquisa, uma perspectiva mais clara.",
  browse: "Explorar as pesquisas",
  latest: "Pesquisas recentes",
  latestNote: "Registros publicados, com suas evidências à vista.",
  publication: "publicação",
  publications: "publicações",
  all: "Todos os temas",
  search: "Buscar títulos, resumos ou temas",
  searchLabel: "Buscar pesquisas",
  reset: "Limpar filtros",
  noResults: "Nenhuma pesquisa encontrada",
  noResultsNote: "Tente outro tema ou uma busca mais ampla.",
  empty: "Ainda não há publicações",
  emptyNote:
    "As pesquisas publicadas aparecerão aqui após a revisão editorial.",
  read: "Ler a análise",
  evidence: "Força da evidência",
  version: "Versão",
  published: "Publicado em",
  back: "Voltar às pesquisas",
  why: "Por que importa",
  claims: "Alegações e evidências",
  claimsNote:
    "O que se afirma, o que sustenta a afirmação e de onde vem esse suporte.",
  claim: "Alegação",
  confidence: "Confiança",
  supportingEvidence: "Evidências de suporte",
  noEvidence: "Nenhuma evidência de suporte foi vinculada a esta alegação.",
  limitations: "Limitações e questões em aberto",
  noLimitations:
    "Nenhuma limitação foi informada nesta publicação. Isso não demonstra que os resultados sejam generalizáveis.",
  sources: "Fontes primárias",
  source: "Fonte",
  sourceDate: "Fonte publicada em",
  openSource: "Abrir fonte",
  history: "Histórico de publicação",
  historyNote:
    "As mudanças ficam visíveis. Esta página mostra a última versão publicada.",
  record: "Registro de pesquisa",
  onThisPage: "Nesta análise",
  topics: "Temas",
  approachTitle: "Clareza é uma prática de pesquisa.",
  approachDescription:
    "Uma boa manchete é um ponto de partida. Mantemos o raciocínio por trás dela visível, para que você avalie o que as evidências realmente sustentam.",
  approachOne: "Separe a alegação",
  approachOneText:
    "Distinga o que os autores afirmam do que foi demonstrado de forma independente.",
  approachTwo: "Rastreie as evidências",
  approachTwoText:
    "Siga cada alegação até sua fonte, com trechos e localizações precisas.",
  approachThree: "Preserve as ressalvas",
  approachThreeText:
    "Leia as limitações junto com o resultado e veja como o registro evolui.",
  asideTitle: "Um caminho de leitura mais claro",
  asideText:
    "Acompanhe o percurso de um resultado até as evidências que o sustentam.",
  asideClaim: "A alegação",
  asideClaimText: "O que está sendo afirmado?",
  asideEvidence: "A evidência",
  asideEvidenceText: "O que dá suporte?",
  asideSource: "A fonte",
  asideSourceText: "Onde você pode verificar?",
  bilingual: "Dois idiomas. Um registro de pesquisa.",
  bilingualText:
    "Inglês e português brasileiro compartilham as mesmas alegações canônicas, citações e versões publicadas.",
  footerNote: "Contexto antes de conclusões. Evidências antes de certezas.",
  status: "Prévia pública · Dados sintéticos de demonstração",
  unavailable: "As pesquisas estão temporariamente indisponíveis",
  unavailableNote:
    "Não foi possível acessar o serviço de pesquisa. Tente novamente em instantes.",
  retry: "Tentar novamente",
  notFound: "Esta página de pesquisa não está disponível",
  notFoundNote:
    "Ela pode não ter sido publicada neste idioma, ou o endereço pode estar incorreto.",
  notFoundGlobal: "Página não encontrada",
  loading: "Carregando pesquisas…",
};
export function dictionary(locale: Locale) {
  return locale === "en" ? en : pt;
}
export function localizedDate(value: string, locale: Locale): string {
  return new Intl.DateTimeFormat(locale, {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(value));
}

const terms: Record<string, [string, string]> = {
  limited: ["Limited", "Limitada"],
  modelrelease: ["Model release", "Lançamento de modelo"],
  syntheticillustration: ["Synthetic illustration", "Ilustração sintética"],
  independentfinding: ["Independent finding", "Resultado independente"],
  syntheticfixture: ["Synthetic fixture", "Dado sintético de teste"],
  vendorreport: ["Vendor report", "Relato do fornecedor"],
  independentreplication: [
    "Independent replication",
    "Replicação independente",
  ],
  high: ["High", "Alta"],
  moderate: ["Moderate", "Moderada"],
  medium: ["Medium", "Média"],
  low: ["Low", "Baixa"],
  strong: ["Strong", "Forte"],
  weak: ["Weak", "Fraca"],
  preliminary: ["Preliminary", "Preliminar"],
  authorreported: ["Author-reported", "Relatada pelos autores"],
  vendorreported: ["Vendor-reported", "Relatada pelo fornecedor"],
  vendorclaim: ["Vendor claim", "Alegação do fornecedor"],
  authorclaim: ["Author claim", "Alegação dos autores"],
  independent: ["Independent", "Independente"],
  independentlyverified: [
    "Independently verified",
    "Verificada de forma independente",
  ],
  reported: ["Reported", "Relatada"],
  unverified: ["Unverified", "Não verificada"],
  synthetic: ["Synthetic", "Sintética"],
  research: ["Research", "Pesquisa"],
  researchitem: ["Research", "Pesquisa"],
  researchpaper: ["Research paper", "Artigo científico"],
  paper: ["Paper", "Artigo"],
  news: ["News", "Notícia"],
  researchnote: ["Research note", "Nota de pesquisa"],
  experiment: ["Experiment", "Experimento"],
  experimental: ["Experimental", "Experimental"],
  benchmark: ["Benchmark", "Benchmark"],
  benchmarkresult: ["Benchmark result", "Resultado de benchmark"],
  sourcestatement: ["Source statement", "Afirmação da fonte"],
  primarysource: ["Primary source", "Fonte primária"],
  empirical: ["Empirical", "Empírica"],
  observation: ["Observation", "Observação"],
  hypothesis: ["Hypothesis", "Hipótese"],
  measured: ["Measured", "Medida"],
};
export function term(value: string, locale: Locale): string {
  const known = terms[value.toLowerCase().replace(/[^a-z]/g, "")];
  return known
    ? known[locale === "en" ? 0 : 1]
    : value.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/[_-]/g, " ");
}
