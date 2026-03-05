'use client'

import * as React from 'react'
import { format, setHours, setMinutes } from 'date-fns'
import { CalendarIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import { Label } from '@/components/ui/label'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'

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
      <Label id={labelId}>{label}</Label>

      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            disabled={disabled}
            aria-labelledby={labelId}
            className={cn(
              'w-full justify-start text-left font-normal',
              !value && 'text-muted-foreground',
            )}
          >
            <CalendarIcon className="size-4 text-muted-foreground" />
            {value
              ? format(value, "MMM d, yyyy 'at' h:mm a")
              : 'Select date and time'}
          </Button>
        </PopoverTrigger>

        <PopoverContent className="w-auto p-0" align="start">
          <div className="p-3">
            <Calendar
              mode="single"
              selected={value ?? undefined}
              onSelect={handleDateSelect}
              autoFocus
            />
          </div>

          <div className="border-t px-3 py-3">
            <div className="flex items-center gap-2">
              <label
                htmlFor={`${labelId}-time`}
                className="text-sm font-medium"
              >
                Time
              </label>
              <input
                id={`${labelId}-time`}
                type="time"
                value={timeValue}
                onChange={handleTimeChange}
                disabled={disabled}
                className={cn(
                  'h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs outline-none',
                  'focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50',
                )}
              />
            </div>
          </div>
        </PopoverContent>
      </Popover>
    </div>
  )
}
