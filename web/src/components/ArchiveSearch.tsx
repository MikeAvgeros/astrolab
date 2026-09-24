import { useState } from 'react';
import { callApi, describeError } from '../api';
import type { StagedFile } from '../files';
import { enumValues, type OpenApiSpec } from '../openapi';

interface Observation {
  datasetId: string;
  target: string;
  instrument: string;
  observationDate?: string | null;
  source: string;
  collection?: string | null;
  dataProductType?: string | null;
  exposureTimeSeconds?: number | null;
  rightAscension?: number | null;
  declination?: number | null;
}

interface Props {
  spec: OpenApiSpec;
  onFileStaged: (file: Omit<StagedFile, 'addedAt'>) => void;
  onOpen: (fileId: string) => void;
}

export function ArchiveSearch({ spec, onFileStaged, onOpen }: Props) {
  const archives = enumValues(spec, 'ArchiveSource');
  const [archive, setArchive] = useState(archives[0] ?? 'Mast');
  const [target, setTarget] = useState('');
  const [mission, setMission] = useState('');
  const [instrument, setInstrument] = useState('');
  const [radius, setRadius] = useState('');
  const [maxResults, setMaxResults] = useState('50');
  const [results, setResults] = useState<Observation[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [searching, setSearching] = useState(false);
  const [downloads, setDownloads] = useState<Record<string, { state: 'busy' | 'done' | 'error'; fileId?: string; message?: string }>>({});

  async function search(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSearching(true);
    try {
      const res = await callApi('GET', '/api/archives/search', {
        query: { archive, target, mission, instrument, searchRadiusDegrees: radius, maxResults },
      });

      if (!res.ok) {
        setError(describeError(res));
        setResults(null);
        return;
      }

      setResults((res.json as { observations: Observation[] }).observations);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSearching(false);
    }
  }

  async function download(obs: Observation) {
    const key = obs.datasetId;
    setDownloads(d => ({ ...d, [key]: { state: 'busy' } }));
    try {
      const res = await callApi('POST', '/api/archives/download', { json: { archive: obs.source, datasetId: obs.datasetId } });
      const payload = res.json as { fileId: string; sizeBytes: number } | undefined;

      if (!res.ok || !payload) {
        setDownloads(d => ({ ...d, [key]: { state: 'error', message: describeError(res) } }));
        return;
      }

      onFileStaged({ fileId: payload.fileId, label: `${obs.target} · ${obs.datasetId}`, sizeBytes: payload.sizeBytes, source: 'archive' });
      setDownloads(d => ({ ...d, [key]: { state: 'done', fileId: payload.fileId } }));
    } catch (err) {
      setDownloads(d => ({ ...d, [key]: { state: 'error', message: (err as Error).message } }));
    }
  }

  return (
    <div className="archives">
      <form className="viewer-controls" onSubmit={search}>
        <label className="field">
          <span>Archive</span>
          <select value={archive} onChange={e => setArchive(e.target.value)}>
            {archives.map(a => <option key={a}>{a}</option>)}
          </select>
        </label>
        <label className="field grow">
          <span>Target <em className="required">*</em></span>
          <input required value={target} onChange={e => setTarget(e.target.value)} placeholder="e.g. M31 or NGC 1300" />
        </label>
        <label className="field">
          <span>Mission / collection</span>
          <input value={mission} onChange={e => setMission(e.target.value)} />
        </label>
        <label className="field">
          <span>Instrument</span>
          <input value={instrument} onChange={e => setInstrument(e.target.value)} />
        </label>
        <label className="field">
          <span>Radius (deg)</span>
          <input type="number" step="any" value={radius} onChange={e => setRadius(e.target.value)} />
        </label>
        <label className="field">
          <span>Max results</span>
          <input type="number" step={1} min={1} value={maxResults} onChange={e => setMaxResults(e.target.value)} />
        </label>
        <div className="actions">
          <button type="submit" disabled={searching}>{searching ? 'Searching…' : 'Search'}</button>
        </div>
      </form>

      {error && <p className="error-text">{error}</p>}

      {results && results.length === 0 && <p className="muted">No observations found.</p>}

      {results && results.length > 0 && (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Dataset</th><th>Target</th><th>Instrument</th><th>Collection</th><th>Type</th>
                <th>Date</th><th>Exposure (s)</th><th>RA / Dec (deg)</th><th></th>
              </tr>
            </thead>
            <tbody>
              {results.map(obs => {
                const dl = downloads[obs.datasetId];
                return (
                  <tr key={`${obs.source}:${obs.datasetId}`}>
                    <td><code>{obs.datasetId}</code></td>
                    <td>{obs.target}</td>
                    <td>{obs.instrument}</td>
                    <td>{obs.collection ?? '—'}</td>
                    <td>{obs.dataProductType ?? '—'}</td>
                    <td>{obs.observationDate ? new Date(obs.observationDate).toISOString().slice(0, 10) : '—'}</td>
                    <td>{obs.exposureTimeSeconds ?? '—'}</td>
                    <td>{obs.rightAscension != null && obs.declination != null ? `${obs.rightAscension.toFixed(5)}, ${obs.declination.toFixed(5)}` : '—'}</td>
                    <td className="nowrap">
                      {dl?.state === 'done' && dl.fileId
                        ? <button type="button" className="link" onClick={() => onOpen(dl.fileId!)}>Open</button>
                        : <button type="button" disabled={dl?.state === 'busy'} onClick={() => void download(obs)}>
                            {dl?.state === 'busy' ? 'Downloading…' : 'Download'}
                          </button>}
                      {dl?.state === 'error' && <div className="error-text">{dl.message}</div>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
