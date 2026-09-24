import { useEffect, useState } from 'react';
import { callApi, type ApiResponse, type QueryValue } from '../api';
import { enumValues, type OpenApiSpec } from '../openapi';
import { ResponseView } from './ResponseView';

type Mode = 'render' | 'render/overlay' | 'render/wcs-grid';

const MODES: { value: Mode; label: string }[] = [
  { value: 'render', label: 'Image' },
  { value: 'render/overlay', label: 'Detected sources overlay' },
  { value: 'render/wcs-grid', label: 'WCS grid' },
];

export function ImageViewer({ spec, fileId }: { spec: OpenApiSpec; fileId: string }) {
  const [mode, setMode] = useState<Mode>('render');
  const [stretch, setStretch] = useState('Asinh');
  const [colorMap, setColorMap] = useState('Grayscale');
  const [lowerPercentile, setLowerPercentile] = useState('1');
  const [upperPercentile, setUpperPercentile] = useState('99');
  const [maxDimension, setMaxDimension] = useState('1024');
  const [thresholdSigma, setThresholdSigma] = useState('5');
  const [image, setImage] = useState<ApiResponse | null>(null);
  const [stats, setStats] = useState<ApiResponse | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => () => { if (image?.blobUrl) URL.revokeObjectURL(image.blobUrl); }, [image]);

  // Clear results when the active file changes.
  useEffect(() => {
    setImage(null);
    setStats(null);
  }, [fileId]);

  async function render(e?: React.FormEvent) {
    e?.preventDefault();

    if (!fileId) return;

    const query: Record<string, QueryValue> = mode === 'render'
      ? { stretch, colorMap, lowerPercentile, upperPercentile, maxDimension }
      : mode === 'render/overlay'
        ? { thresholdSigma }
        : {};

    setBusy(true);
    try {
      const base = `/api/images/${encodeURIComponent(fileId)}`;
      const [img, st] = await Promise.all([
        callApi('GET', `${base}/${mode}`, { query }),
        callApi('GET', `${base}/statistics`),
      ]);
      setImage(img);
      setStats(st);
    } finally {
      setBusy(false);
    }
  }

  if (!fileId) return <p className="muted">Select or upload a FITS file to view it.</p>;

  const stretchModes = enumValues(spec, 'StretchMode');
  const colorMaps = enumValues(spec, 'ColorMap');

  return (
    <div className="viewer">
      <form className="viewer-controls" onSubmit={render}>
        <label className="field">
          <span>View</span>
          <select value={mode} onChange={e => setMode(e.target.value as Mode)}>
            {MODES.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
          </select>
        </label>
        {mode === 'render' && (
          <>
            <label className="field">
              <span>Stretch</span>
              <select value={stretch} onChange={e => setStretch(e.target.value)}>
                {stretchModes.map(v => <option key={v}>{v}</option>)}
              </select>
            </label>
            <label className="field">
              <span>Colour map</span>
              <select value={colorMap} onChange={e => setColorMap(e.target.value)}>
                {colorMaps.map(v => <option key={v}>{v}</option>)}
              </select>
            </label>
            <label className="field">
              <span>Lower %</span>
              <input type="number" step="any" value={lowerPercentile} onChange={e => setLowerPercentile(e.target.value)} />
            </label>
            <label className="field">
              <span>Upper %</span>
              <input type="number" step="any" value={upperPercentile} onChange={e => setUpperPercentile(e.target.value)} />
            </label>
            <label className="field">
              <span>Max dimension</span>
              <input type="number" step={1} value={maxDimension} onChange={e => setMaxDimension(e.target.value)} />
            </label>
          </>
        )}
        {mode === 'render/overlay' && (
          <label className="field">
            <span>Threshold σ</span>
            <input type="number" step="any" value={thresholdSigma} onChange={e => setThresholdSigma(e.target.value)} />
          </label>
        )}
        <div className="actions">
          <button type="submit" disabled={busy}>{busy ? 'Rendering…' : 'Render'}</button>
        </div>
      </form>

      {image && <ResponseView response={image} />}
      {stats && (
        <details className="stats" open>
          <summary>Image statistics</summary>
          <ResponseView response={stats} />
        </details>
      )}
    </div>
  );
}
