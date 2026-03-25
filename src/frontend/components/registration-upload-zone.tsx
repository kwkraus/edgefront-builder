'use client'

import { useState, useCallback } from 'react'

interface RegistrationUploadZoneProps {
  onFileSelected: (file: File) => void
}

export function RegistrationUploadZone({ onFileSelected }: RegistrationUploadZoneProps) {
  const [isDragActive, setIsDragActive] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const validateFile = useCallback((file: File): boolean => {
    if (!file.name.toLowerCase().endsWith('.csv')) {
      setError('Please upload a CSV file')
      return false
    }
    if (file.size === 0) {
      setError('File is empty')
      return false
    }
    setError(null)
    return true
  }, [])

  const handleDrop = useCallback(
    (e: React.DragEvent<HTMLDivElement>) => {
      e.preventDefault()
      e.stopPropagation()
      setIsDragActive(false)

      const files = e.dataTransfer.files
      if (files.length > 0) {
        const file = files[0]
        if (validateFile(file)) {
          onFileSelected(file)
        }
      }
    },
    [validateFile, onFileSelected],
  )

  const handleDragOver = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragActive(true)
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragActive(false)
  }, [])

  const handleFileInputChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const files = e.currentTarget.files
      if (files && files.length > 0) {
        const file = files[0]
        if (validateFile(file)) {
          onFileSelected(file)
        }
      }
    },
    [validateFile, onFileSelected],
  )

  return (
    <div
      onDrop={handleDrop}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      className={`
        relative rounded-lg border-2 border-dashed p-8 text-center transition-colors
        ${
          isDragActive
            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
            : 'border-gray-300 bg-gray-50 dark:bg-gray-900/20'
        }
      `}
      style={{
        borderColor: isDragActive
          ? 'var(--borderColor-accent, var(--color-accent-emphasis))'
          : 'var(--borderColor-default, var(--color-border-default))',
        backgroundColor: isDragActive
          ? 'var(--bgColor-accent-muted, var(--color-accent-subtle))'
          : 'var(--bgColor-muted, var(--color-canvas-subtle))',
      }}
    >
      <input
        type="file"
        accept=".csv"
        onChange={handleFileInputChange}
        className="absolute inset-0 cursor-pointer opacity-0"
        aria-label="Upload registration CSV file"
      />

      <div className="space-y-2">
        <p className="text-sm font-semibold">Drag and drop your registration CSV here</p>
        <p className="text-xs" style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>
          or click to select a file
        </p>
      </div>

      {error && (
        <p className="mt-3 text-sm" style={{ color: 'var(--fgColor-danger, var(--color-danger-fg))' }}>
          {error}
        </p>
      )}
    </div>
  )
}
