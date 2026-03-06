'use client'

import * as React from 'react'
import { useSession } from 'next-auth/react'
import { SearchIcon } from '@primer/octicons-react'
import { ActionList, AnchoredOverlay, Spinner, Text, TextInput, Token } from '@primer/react'
import { searchPeople } from '@/lib/api/sessions'
import type { PersonInput, PersonSearchResult } from '@/lib/api/types'

interface PeoplePickerProps {
  label: string
  value: PersonInput[]
  onChange: (people: PersonInput[]) => void
  disabled?: boolean
  hideLabel?: boolean
}

export function PeoplePicker({
  label,
  value,
  onChange,
  disabled = false,
  hideLabel = false,
}: PeoplePickerProps) {
  const { data: session } = useSession()
  const [open, setOpen] = React.useState(false)
  const [query, setQuery] = React.useState('')
  const [results, setResults] = React.useState<PersonSearchResult[]>([])
  const [loading, setLoading] = React.useState(false)
  const inputRef = React.useRef<HTMLInputElement>(null)

  const selectedIds = React.useMemo(
    () => new Set(value.map((p) => p.entraUserId)),
    [value],
  )

  // Debounced search (300 ms)
  React.useEffect(() => {
    if (query.length < 2) {
      setResults([])
      return
    }

    const accessToken = (session as { accessToken?: string } | null)
      ?.accessToken
    if (!accessToken) return

    const timer = setTimeout(async () => {
      setLoading(true)
      try {
        const data = await searchPeople(query, accessToken)
        setResults(data)
      } catch {
        setResults([])
      } finally {
        setLoading(false)
      }
    }, 300)

    return () => clearTimeout(timer)
  }, [query, session])

  const filteredResults = results.filter((r) => !selectedIds.has(r.entraUserId))

  const showDropdown = open && query.length >= 2

  function handleSelect(person: PersonSearchResult) {
    onChange([
      ...value,
      {
        entraUserId: person.entraUserId,
        displayName: person.displayName,
        email: person.email,
      },
    ])
    setQuery('')
    setResults([])
    setOpen(false)
    inputRef.current?.focus()
  }

  function handleRemove(entraUserId: string) {
    onChange(value.filter((p) => p.entraUserId !== entraUserId))
  }

  const labelId = React.useId()

  return (
    <div
      style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}
      data-disabled={disabled || undefined}
    >
      <Text
        as="label"
        id={labelId}
        size="small"
        weight="semibold"
        className={hideLabel ? 'sr-only' : undefined}
      >
        {label}
      </Text>

      <AnchoredOverlay
        open={showDropdown}
        onOpen={() => {
          if (query.length >= 2) setOpen(true)
        }}
        onClose={() => setOpen(false)}
        focusTrapSettings={{ disabled: true }}
        focusZoneSettings={{ disabled: true }}
        renderAnchor={(anchorProps) => (
          <div {...anchorProps}>
            <TextInput
              ref={inputRef}
              leadingVisual={SearchIcon}
              placeholder={`Search ${label.toLowerCase()}…`}
              value={query}
              disabled={disabled}
              role="combobox"
              aria-expanded={showDropdown}
              aria-controls={`${labelId}-listbox`}
              aria-labelledby={labelId}
              aria-autocomplete="list"
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                setQuery(e.target.value)
                if (e.target.value.length >= 2) {
                  setOpen(true)
                }
              }}
              onFocus={() => {
                if (query.length >= 2) setOpen(true)
              }}
              onKeyDown={(e: React.KeyboardEvent<HTMLInputElement>) => {
                if (e.key === 'Escape') {
                  setOpen(false)
                }
              }}
              block
            />
          </div>
        )}
      >
        <ActionList id={`${labelId}-listbox`} role="listbox">
          {loading && (
            <div style={{ padding: '12px 0', textAlign: 'center', color: 'var(--fgColor-muted)', fontSize: '14px' }}>
              <Spinner size="small" />
            </div>
          )}

          {!loading && showDropdown && filteredResults.length === 0 && (
            <div style={{ padding: '12px 0', textAlign: 'center', color: 'var(--fgColor-muted)', fontSize: '14px' }}>
              No results found
            </div>
          )}

          {!loading &&
            filteredResults.map((person) => (
              <ActionList.Item
                key={person.entraUserId}
                onSelect={() => handleSelect(person)}
                role="option"
              >
                {person.displayName}
                <ActionList.Description variant="block">
                  {person.email}
                </ActionList.Description>
              </ActionList.Item>
            ))}
        </ActionList>
      </AnchoredOverlay>

      {value.length > 0 && (
        <div
          style={{ display: 'flex', flexWrap: 'wrap', gap: '6px' }}
          role="list"
          aria-label={`Selected ${label.toLowerCase()}`}
        >
          {value.map((person) => (
            <Token
              key={person.entraUserId}
              text={person.displayName}
              onRemove={disabled ? undefined : () => handleRemove(person.entraUserId)}
            />
          ))}
        </div>
      )}
    </div>
  )
}
