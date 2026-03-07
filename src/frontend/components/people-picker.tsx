'use client'

import * as React from 'react'
import { useSession } from 'next-auth/react'
import { SearchIcon } from '@primer/octicons-react'
import { ActionList, Spinner, Text, TextInput, Token } from '@primer/react'
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
  const [query, setQuery] = React.useState('')
  const [results, setResults] = React.useState<PersonSearchResult[]>([])
  const [loading, setLoading] = React.useState(false)
  const [focused, setFocused] = React.useState(false)
  const inputRef = React.useRef<HTMLInputElement>(null)
  const containerRef = React.useRef<HTMLDivElement>(null)

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

  // Close dropdown when clicking outside
  React.useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setFocused(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const filteredResults = results.filter((r) => !selectedIds.has(r.entraUserId))
  const showDropdown = focused && query.length >= 2

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

      <div ref={containerRef} style={{ position: 'relative' }}>
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
          }}
          onFocus={() => setFocused(true)}
          onKeyDown={(e: React.KeyboardEvent<HTMLInputElement>) => {
            if (e.key === 'Escape') {
              setFocused(false)
              inputRef.current?.blur()
            }
          }}
          block
        />

        {showDropdown && (
          <div
            style={{
              position: 'absolute',
              top: '100%',
              left: 0,
              right: 0,
              zIndex: 100,
              marginTop: '4px',
              backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))',
              border: '1px solid var(--borderColor-default, var(--color-border-default))',
              borderRadius: 'var(--borderRadius-medium, 6px)',
              boxShadow: 'var(--shadow-floating-small, var(--color-shadow-medium))',
              maxHeight: '240px',
              overflowY: 'auto',
            }}
          >
            <ActionList id={`${labelId}-listbox`} role="listbox">
              {loading && (
                <div style={{ padding: '12px 0', textAlign: 'center', color: 'var(--fgColor-muted)', fontSize: '14px' }}>
                  <Spinner size="small" />
                </div>
              )}

              {!loading && filteredResults.length === 0 && (
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
          </div>
        )}
      </div>

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
