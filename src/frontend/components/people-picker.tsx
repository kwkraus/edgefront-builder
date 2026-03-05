'use client'

import * as React from 'react'
import { useSession } from 'next-auth/react'
import { Search, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { searchPeople } from '@/lib/api/sessions'
import type { PersonInput, PersonSearchResult } from '@/lib/api/types'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import {
  Popover,
  PopoverAnchor,
  PopoverContent,
} from '@/components/ui/popover'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'

interface PeoplePickerProps {
  label: string
  value: PersonInput[]
  onChange: (people: PersonInput[]) => void
  disabled?: boolean
}

export function PeoplePicker({
  label,
  value,
  onChange,
  disabled = false,
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

  // Debounced search
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
    <div className="flex flex-col gap-2" data-disabled={disabled || undefined}>
      <Label id={labelId}>{label}</Label>

      <Popover open={open && query.length >= 2} onOpenChange={setOpen}>
        <PopoverAnchor asChild>
          <div
            className={cn(
              'flex items-center gap-2 rounded-md border border-input bg-transparent px-3 shadow-xs transition-[color,box-shadow]',
              'focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/50',
              disabled && 'pointer-events-none opacity-50',
            )}
          >
            <Search className="size-4 shrink-0 text-muted-foreground" />
            <input
              ref={inputRef}
              type="text"
              role="combobox"
              aria-expanded={open && query.length >= 2}
              aria-controls={`${labelId}-listbox`}
              aria-labelledby={labelId}
              aria-autocomplete="list"
              placeholder={`Search ${label.toLowerCase()}…`}
              value={query}
              disabled={disabled}
              onChange={(e) => {
                setQuery(e.target.value)
                if (e.target.value.length >= 2) {
                  setOpen(true)
                }
              }}
              onFocus={() => {
                if (query.length >= 2) setOpen(true)
              }}
              onKeyDown={(e) => {
                if (e.key === 'Escape') {
                  setOpen(false)
                }
              }}
              className="h-9 w-full min-w-0 bg-transparent py-1 text-sm outline-none placeholder:text-muted-foreground"
            />
          </div>
        </PopoverAnchor>

        <PopoverContent
          className="w-[var(--radix-popover-trigger-width)] p-0"
          align="start"
          onOpenAutoFocus={(e) => e.preventDefault()}
          onCloseAutoFocus={(e) => e.preventDefault()}
        >
          <Command shouldFilter={false}>
            <CommandList id={`${labelId}-listbox`} role="listbox">
              {loading && (
                <div className="py-4 text-center text-sm text-muted-foreground">
                  Searching…
                </div>
              )}
              {!loading && query.length >= 2 && filteredResults.length === 0 && (
                <CommandEmpty>No results found</CommandEmpty>
              )}
              {!loading && filteredResults.length > 0 && (
                <CommandGroup>
                  {filteredResults.map((person) => (
                    <CommandItem
                      key={person.entraUserId}
                      value={person.entraUserId}
                      onSelect={() => handleSelect(person)}
                    >
                      <div className="flex flex-col gap-0.5">
                        <span className="text-sm font-medium">
                          {person.displayName}
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {person.email}
                        </span>
                      </div>
                    </CommandItem>
                  ))}
                </CommandGroup>
              )}
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>

      {value.length > 0 && (
        <div
          className="flex flex-wrap gap-1.5"
          role="list"
          aria-label={`Selected ${label.toLowerCase()}`}
        >
          {value.map((person) => (
            <Badge
              key={person.entraUserId}
              variant="secondary"
              className="gap-1 pr-1"
              role="listitem"
            >
              {person.displayName}
              <button
                type="button"
                aria-label={`Remove ${person.displayName}`}
                disabled={disabled}
                onClick={() => handleRemove(person.entraUserId)}
                className="rounded-full p-0.5 hover:bg-muted-foreground/20 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              >
                <X className="size-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}
    </div>
  )
}
