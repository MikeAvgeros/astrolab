import { useEffect, useState } from 'react';

// The API has no "list staged files" endpoint, so the SPA remembers the file IDs it has seen.
export interface StagedFile {
  fileId: string;
  label: string;
  sizeBytes?: number;
  source: 'upload' | 'archive' | 'manual';
  addedAt: string;
}

const STORAGE_KEY = 'astrolab.files';

function read(): StagedFile[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as StagedFile[]) : [];
  } catch {
    return [];
  }
}

export function useStagedFiles() {
  const [files, setFiles] = useState<StagedFile[]>(read);

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(files));
    } catch {
      // Storage unavailable (private mode etc.); keep in memory only.
    }
  }, [files]);

  const add = (file: Omit<StagedFile, 'addedAt'>) =>
    setFiles(prev => [{ ...file, addedAt: new Date().toISOString() }, ...prev.filter(f => f.fileId !== file.fileId)]);

  const remove = (fileId: string) => setFiles(prev => prev.filter(f => f.fileId !== fileId));

  return { files, add, remove };
}

export function formatBytes(bytes?: number): string {
  if (bytes === undefined) return '';

  const units = ['B', 'KB', 'MB', 'GB'];
  let value = bytes;
  let unit = 0;

  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit++;
  }

  return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
}
