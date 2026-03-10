'use client'

import * as React from 'react'
import { ClockIcon } from '@primer/octicons-react'
import { ActionList, ActionMenu, Text } from '@primer/react'
import { nearestSlotIndex, type TimeSlot, type EndTimeSlot } from '@/lib/time-utils'

type Slot = TimeSlot | EndTimeSlot

interface TimePickerProps {
  label: string
  value: { hours: number; minutes: number } | null
  onChange: (time: { hours: number; minutes: number }) => void
  slots: Slot[]
  disabled?: boolean
  placeholder?: string
}

function displayLabel(slot: Slot): string {
  if ('durationLabel' in slot) return slot.durationLabel
  return slot.label
}

export function TimePicker({
  label,
  value,
  onChange,
  slots,
  disabled = false,
  placeholder = 'Select time',
}: TimePickerProps) {
  const labelId = React.useId()
  const listRef = React.useRef<HTMLDivElement>(null)

  const selectedValue = value
    ? `${String(value.hours).padStart(2, '0')}:${String(value.minutes).padStart(2, '0')}`
    : null

  const selectedSlot = selectedValue
    ? slots.find((s) => s.value === selectedValue)
    : null

  // Scroll to the selected slot (if any), otherwise scroll to the nearest slot
  function handleOpenChange(open: boolean) {
    if (open) {
      requestAnimationFrame(() => {
        const container = listRef.current
        if (!container) return
        const active = container.querySelector('[data-active="true"]')
        if (active) {
          active.scrollIntoView({ block: 'center' })
        } else if (slots.length > 0) {
          // No selection: scroll to the nearest slot to current time
          const now = new Date()
          const idx = nearestSlotIndex(now.getHours(), now.getMinutes(), slots)
          const items = container.querySelectorAll('[role="option"]')
          if (idx >= 0 && idx < items.length) {
            items[idx]?.scrollIntoView({ block: 'center' })
          }
        }
      })
    }
  }

  return (
    <div className="flex flex-col gap-1">
      <Text id={labelId} size="small" weight="semibold">
        {label}
      </Text>

      <ActionMenu onOpenChange={handleOpenChange}>
        <ActionMenu.Button
          leadingVisual={ClockIcon}
          disabled={disabled}
          aria-labelledby={labelId}
          style={{
            justifyContent: 'flex-start',
            fontWeight: 'normal',
            color: selectedSlot
              ? 'var(--fgColor-default)'
              : 'var(--fgColor-muted)',
          }}
        >
          {selectedSlot ? displayLabel(selectedSlot) : placeholder}
        </ActionMenu.Button>
        <ActionMenu.Overlay
          width="auto"
          style={{ maxHeight: '280px', overflowY: 'auto' }}
        >
          <div ref={listRef}>
            <ActionList selectionVariant="single">
              {slots.map((slot) => (
                <ActionList.Item
                  key={slot.value}
                  selected={slot.value === selectedValue}
                  data-active={slot.value === selectedValue ? 'true' : undefined}
                  onSelect={() => onChange({ hours: slot.hours, minutes: slot.minutes })}
                >
                  {displayLabel(slot)}
                </ActionList.Item>
              ))}
            </ActionList>
          </div>
        </ActionMenu.Overlay>
      </ActionMenu>
    </div>
  )
}
