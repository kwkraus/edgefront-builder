'use client'

import * as React from 'react'
import { format } from 'date-fns'
import { CalendarIcon } from '@primer/octicons-react'
import { AnchoredOverlay, Button, Text } from '@primer/react'
import { DayPicker } from 'react-day-picker'

interface DatePickerProps {
  label: string
  value: Date | null
  onChange: (date: Date | null) => void
  disabled?: boolean
}

export function DatePicker({
  label,
  value,
  onChange,
  disabled = false,
}: DatePickerProps) {
  const [open, setOpen] = React.useState(false)
  const labelId = React.useId()

  function handleSelect(day: Date | undefined) {
    onChange(day ?? null)
    setOpen(false)
  }

  return (
    <div className="flex flex-col gap-1">
      <Text id={labelId} size="small" weight="semibold">
        {label}
      </Text>

      <AnchoredOverlay
        open={open}
        onOpen={() => setOpen(true)}
        onClose={() => setOpen(false)}
        renderAnchor={(anchorProps) => (
          <Button
            {...anchorProps}
            leadingVisual={CalendarIcon}
            variant="default"
            disabled={disabled}
            aria-labelledby={labelId}
            style={{
              justifyContent: 'flex-start',
              fontWeight: 'normal',
              color: value
                ? 'var(--fgColor-default)'
                : 'var(--fgColor-muted)',
            }}
          >
            {value ? format(value, 'MMM d, yyyy') : 'Select date'}
          </Button>
        )}
      >
        <div
          className="p-3"
          style={
            {
              '--rdp-accent-color': 'var(--bgColor-accent-emphasis)',
              '--rdp-accent-background-color': 'var(--bgColor-accent-muted)',
              '--rdp-today-color': 'var(--fgColor-accent)',
            } as React.CSSProperties
          }
        >
          <DayPicker
            mode="single"
            selected={value ?? undefined}
            onSelect={handleSelect}
            showOutsideDays
            autoFocus
          />
        </div>
      </AnchoredOverlay>
    </div>
  )
}
