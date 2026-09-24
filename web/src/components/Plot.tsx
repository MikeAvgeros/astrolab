import { useState } from 'react';

export type Series = Record<string, number[]>;

const WIDTH = 720;
const HEIGHT = 320;
const PAD = { top: 12, right: 16, bottom: 36, left: 64 };
const INDEX = '(index)';

/** Plots any two equal-length numeric series found in a JSON response. */
export function Plot({ series, initialMode = 'line' }: { series: Series; initialMode?: 'line' | 'points' }) {
  const names = Object.keys(series);
  const [yName, setYName] = useState(() => pickDefaultY(names));
  const [xName, setXName] = useState(() => pickDefaultX(names, series, pickDefaultY(names)));
  const [mode, setMode] = useState(initialMode);

  const y = series[yName] ?? [];
  const xOptions = [INDEX, ...names.filter(n => n !== yName && series[n].length === y.length)];
  const x = xName !== INDEX && series[xName]?.length === y.length ? series[xName] : y.map((_, i) => i);

  const geometry = project(x, y);

  if (names.length === 0) return null;

  return (
    <div className="plot">
      <div className="plot-controls">
        <label>
          Y
          <select value={yName} onChange={e => setYName(e.target.value)}>
            {names.map(n => <option key={n}>{n}</option>)}
          </select>
        </label>
        <label>
          X
          <select value={xOptions.includes(xName) ? xName : INDEX} onChange={e => setXName(e.target.value)}>
            {xOptions.map(n => <option key={n}>{n}</option>)}
          </select>
        </label>
        <label>
          Style
          <select value={mode} onChange={e => setMode(e.target.value as 'line' | 'points')}>
            <option value="line">line</option>
            <option value="points">points</option>
          </select>
        </label>
        <span className="muted">{geometry.count} finite points</span>
      </div>
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} className="plot-svg" role="img" aria-label={`${yName} vs ${xName}`}>
        <rect x={PAD.left} y={PAD.top} width={WIDTH - PAD.left - PAD.right} height={HEIGHT - PAD.top - PAD.bottom} className="plot-frame" />
        {geometry.xTicks.map(t => (
          <g key={`x${t.pos}`}>
            <line x1={t.pos} x2={t.pos} y1={HEIGHT - PAD.bottom} y2={HEIGHT - PAD.bottom + 4} className="plot-axis" />
            <text x={t.pos} y={HEIGHT - PAD.bottom + 16} textAnchor="middle" className="plot-label">{t.label}</text>
          </g>
        ))}
        {geometry.yTicks.map(t => (
          <g key={`y${t.pos}`}>
            <line x1={PAD.left - 4} x2={PAD.left} y1={t.pos} y2={t.pos} className="plot-axis" />
            <text x={PAD.left - 6} y={t.pos + 4} textAnchor="end" className="plot-label">{t.label}</text>
          </g>
        ))}
        {mode === 'line'
          ? <path d={geometry.path} className="plot-line" />
          : geometry.points.map(([px, py], i) => <circle key={i} cx={px} cy={py} r={1.8} className="plot-point" />)}
        <text x={(WIDTH + PAD.left) / 2} y={HEIGHT - 4} textAnchor="middle" className="plot-title">{xOptions.includes(xName) ? xName : INDEX}</text>
      </svg>
    </div>
  );
}

function pickDefaultY(names: string[]): string {
  return names.find(n => /flux|counts|value/i.test(n)) ?? names[names.length - 1] ?? '';
}

function pickDefaultX(names: string[], series: Series, y: string): string {
  const len = series[y]?.length;

  return names.find(n => n !== y && series[n].length === len && /time|wavelength|phase|period|frequency/i.test(n)) ?? INDEX;
}

function project(xs: number[], ys: number[]) {
  const pairs: [number, number][] = [];

  for (let i = 0; i < ys.length; i++) {
    if (Number.isFinite(xs[i]) && Number.isFinite(ys[i])) pairs.push([xs[i], ys[i]]);
  }

  const [xMin, xMax] = extent(pairs.map(p => p[0]));
  const [yMin, yMax] = extent(pairs.map(p => p[1]));
  const plotW = WIDTH - PAD.left - PAD.right;
  const plotH = HEIGHT - PAD.top - PAD.bottom;
  const sx = (v: number) => PAD.left + ((v - xMin) / (xMax - xMin || 1)) * plotW;
  const sy = (v: number) => PAD.top + plotH - ((v - yMin) / (yMax - yMin || 1)) * plotH;

  // Very long series are decimated for drawing only; the underlying data is untouched.
  const step = Math.max(1, Math.floor(pairs.length / 4000));
  const points: [number, number][] = [];

  for (let i = 0; i < pairs.length; i += step) points.push([sx(pairs[i][0]), sy(pairs[i][1])]);

  const path = points.map(([px, py], i) => `${i === 0 ? 'M' : 'L'}${px.toFixed(1)},${py.toFixed(1)}`).join('');

  return {
    count: pairs.length,
    points,
    path,
    xTicks: ticks(xMin, xMax).map(v => ({ pos: sx(v), label: formatTick(v) })),
    yTicks: ticks(yMin, yMax).map(v => ({ pos: sy(v), label: formatTick(v) })),
  };
}

function extent(values: number[]): [number, number] {
  if (values.length === 0) return [0, 1];

  let min = Infinity;
  let max = -Infinity;

  for (const v of values) {
    if (v < min) min = v;
    if (v > max) max = v;
  }

  return [min, max];
}

function ticks(min: number, max: number, count = 5): number[] {
  if (min === max) return [min];

  return Array.from({ length: count }, (_, i) => min + ((max - min) * i) / (count - 1));
}

function formatTick(v: number): string {
  const abs = Math.abs(v);

  if (abs !== 0 && (abs < 1e-3 || abs >= 1e5)) return v.toExponential(2);

  return Number(v.toPrecision(4)).toString();
}
