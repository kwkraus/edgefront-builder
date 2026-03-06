'use client'

import * as React from 'react'
import { format, setHours, setMinutes } from 'date-fns'
import { CalendarIcon } from '@primer/octicons-react'
import { AnchoredOverlay, Button, Text } from '@primer/react'
import { DayPicker } from 'react-day-picker'
import 'react-day-picker/style.css'

interface DateTimePickerProps {
  label: string
  value: Date | null
  onChange: (date: Date | null) => void
  disabled?: boolean
}

export function DateTimePicker({
  label,
  value,
  onChange,
  disabled = false,
}: DateTimePickerProps) {
  const [open, setOpen] = React.useState(false)

  const hours = value ? value.getHours() : 0
  const minutes = value ? value.getMinutes() : 0

  function handleDateSelect(day: Date | undefined) {
    if (!day) {
      onChange(null)
      return
    }

    // Preserve existing time when selecting a new date
    const updated = setMinutes(setHours(day, hours), minutes)
    onChange(updated)
  }

  function handleTimeChange(e: React.ChangeEvent<HTMLInputElement>) {
    const [h, m] = e.target.value.split(':').map(Number)
    if (Number.isNaN(h) || Number.isNaN(m)) return

    if (value) {
      onChange(setMinutes(setHours(new Date(value), h), m))
    } else {
      // If no date yet, use today
      const today = new Date()
      onChange(setMinutes(setHours(today, h), m))
    }
  }

  const timeValue = `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
  const labelId = React.useId()

  return (
    <div className="flex flex-col gap-2" data-disabled={disabled || undefined}>
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
            block
            style={{
              justifyContent: 'flex-start',
              fontWeight: 'normal',
              color: value
                ? 'var(--fgColor-default)'
                : 'var(--fgColor-muted)',
            }}
          >
            {value
              ? format(value, "MMM d, yyyy 'at' h:mm a")
              : 'Select date and time'}
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
            onSelect={handleDateSelect}
            showOutsideDays
            autoFocus
          />
        </div>

        <div
          className="flex items-center gap-2 px-3 py-3"
          style={{ borderTop: '1px solid var(--borderColor-default)' }}
        >
          <label
            htmlFor={`${labelId}-time`}
            className="text-sm font-semibold"
          >
            Time
          </label>
          <input
            id={`${labelId}-time`}
            type="time"
            value={timeValue}
            onChange={handleTimeChange}
            disabled={disabled}
            className="h-8 rounded-md px-3 py-1 text-sm outline-none focus-visible:outline-[var(--borderColor-accent-emphasis)] focus-visible:-outline-offset-1"
            style={{
              border: '1px solid var(--borderColor-default)',
              backgroundColor: 'var(--bgColor-default)',
              color: 'var(--fgColor-default)',
              fontFamily: 'var(--fontStack-system)',
            }}
          />
        </div>
      </AnchoredOverlay>
    </div>
  )
}
