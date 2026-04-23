'use client'

import { useEffect, useRef, useState } from 'react'
import { CheckIcon, PencilIcon } from '@primer/octicons-react'
import { IconButton, Spinner, TextInput } from '@primer/react'

interface InlineEditableTitleProps {
  value: string
  onSave: (nextValue: string) => Promise<void>
  disabled?: boolean
  editAriaLabel: string
  saveAriaLabel: string
  inputAriaLabel: string
  titleClassName?: string
}

export function InlineEditableTitle({
  value,
  onSave,
  disabled = false,
  editAriaLabel,
  saveAriaLabel,
  inputAriaLabel,
  titleClassName,
}: InlineEditableTitleProps) {
  const inputRef = useRef<HTMLInputElement | null>(null)
  const ignoreBlurRef = useRef(false)
  const [isEditing, setIsEditing] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [draftValue, setDraftValue] = useState(value)

  useEffect(() => {
    if (!isEditing) {
      setDraftValue(value)
    }
  }, [isEditing, value])

  useEffect(() => {
    if (isEditing && inputRef.current) {
      inputRef.current.focus()
      inputRef.current.select()
    }
  }, [isEditing])

  const trimmedDraftValue = draftValue.trim()
  const canSave = !disabled && !isSaving && trimmedDraftValue.length > 0 && trimmedDraftValue !== value

  function startEditing() {
    if (disabled) {
      return
    }

    setDraftValue(value)
    setIsEditing(true)
  }

  function cancelEditing() {
    setDraftValue(value)
    setIsEditing(false)
  }

  async function handleSave() {
    if (!canSave) {
      return
    }

    setIsSaving(true)
    try {
      await onSave(trimmedDraftValue)
      setIsEditing(false)
    } catch {
      // The parent handles error presentation; keep the user in edit mode.
    } finally {
      ignoreBlurRef.current = false
      setIsSaving(false)
    }
  }

  function handleInputBlur() {
    if (ignoreBlurRef.current) {
      ignoreBlurRef.current = false
      return
    }

    cancelEditing()
  }

  const TitleTag = 'h1'

  return (
    <div className="flex items-center gap-3">
      {isEditing ? (
        <TextInput
          ref={inputRef}
          value={draftValue}
          onChange={(event) => setDraftValue(event.target.value)}
          onBlur={handleInputBlur}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault()
              void handleSave()
            }

            if (event.key === 'Escape') {
              event.preventDefault()
              cancelEditing()
            }
          }}
          aria-label={inputAriaLabel}
          disabled={disabled || isSaving}
          className="min-w-[16rem]"
          style={{ fontSize: '1.5rem', fontWeight: 700, lineHeight: '2rem' }}
        />
      ) : (
        <TitleTag className={titleClassName}>{value}</TitleTag>
      )}

      {isSaving && <Spinner size="small" />}
      <IconButton
        icon={isEditing ? CheckIcon : PencilIcon}
        aria-label={isEditing ? saveAriaLabel : editAriaLabel}
        variant="invisible"
        size="small"
        disabled={disabled || (isEditing && !canSave)}
        aria-busy={isSaving}
        onMouseDown={isEditing ? (event) => {
          ignoreBlurRef.current = true
          event.preventDefault()
        } : undefined}
        onClick={isEditing ? () => { void handleSave() } : startEditing}
      />
    </div>
  )
}