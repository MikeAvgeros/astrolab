export type QueryValue = string | string[] | undefined;

export interface ApiResponse {
  method: string;
  url: string;
  status: number;
  ok: boolean;
  contentType: string;
  durationMs: number;
  json?: unknown;
  text?: string;
  blobUrl?: string;
}

export interface CallOptions {
  query?: Record<string, QueryValue>;
  json?: unknown;
  rawBody?: Blob;
  signal?: AbortSignal;
}

export function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(query ?? {})) {
    if (value === undefined || value === '') continue;

    // ASP.NET Core binds array query parameters from repeated keys (?levels=1&levels=2).
    for (const item of Array.isArray(value) ? value : [value]) params.append(key, item);
  }

  const qs = params.toString();

  return qs ? `${path}?${qs}` : path;
}

export async function callApi(method: string, path: string, options: CallOptions = {}): Promise<ApiResponse> {
  const url = buildUrl(path, options.query);
  const headers: Record<string, string> = {};
  let body: BodyInit | undefined;

  if (options.rawBody) {
    headers['Content-Type'] = 'application/octet-stream';
    body = options.rawBody;
  } else if (options.json !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(options.json);
  }

  const started = performance.now();
  const res = await fetch(url, { method, headers, body, signal: options.signal });
  const contentType = res.headers.get('content-type') ?? '';
  const result: ApiResponse = {
    method,
    url,
    status: res.status,
    ok: res.ok,
    contentType,
    durationMs: 0,
  };

  if (contentType.includes('json')) {
    const text = await res.text();
    result.text = text;
    try {
      result.json = JSON.parse(text);
    } catch {
      // Leave as text.
    }
  } else if (contentType.startsWith('image/') || contentType.includes('octet-stream')) {
    result.blobUrl = URL.createObjectURL(await res.blob());
  } else {
    result.text = await res.text();
  }

  result.durationMs = Math.round(performance.now() - started);

  return result;
}

/** Extracts a readable message from an RFC 7807 problem response or plain-text error. */
export function describeError(response: ApiResponse): string {
  const problem = response.json as { title?: string; detail?: string; errors?: unknown } | undefined;

  if (problem && (problem.title || problem.detail)) {
    return [problem.title, problem.detail].filter(Boolean).join(': ');
  }

  return response.text?.trim() || `HTTP ${response.status}`;
}
