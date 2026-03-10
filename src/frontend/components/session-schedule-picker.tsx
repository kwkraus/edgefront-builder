'use client'

import * as React from 'react'
import { setHours, setMinutes } from 'date-fns'
import { DatePicker } from '@/components/date-picker'
import { TimePicker } from '@/components/time-picker'
import {
  generateTimeSlots,
  getEndTimeSlots,
  type TimeSlot,
  type EndTimeSlot,
} from '@/lib/time-utils'

const ALL_SLOTS = generateTimeSlots(30)
const DEFAULT_DURATION_MINUTES = 60

interface SessionSchedulePickerProps {
  startsAt: Date | null
  endsAt: Date | null
  onStartsAtChange: (d: Date | null) => void
  onEndsAtChange: (d: Date | null) => void
  disabled?: boolean
}

function timeFromDate(d: Date | null): { hours: number; minutes: number } | null {
  if (!d) return null
  return { hours: d.getHours(), minutes: d.getMinutes() }
}

function dateOnly(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate())
}

export function SessionSchedulePicker({
  startsAt,
  endsAt,
  onStartsAtChange,
  onEndsAtChange,
  disabled = false,
}: SessionSchedulePickerProps) {
  // The shared date for same-day sessions
  const sessionDate = startsAt ?? endsAt

  const startTime = timeFromDate(startsAt)
  const endTime = timeFromDate(endsAt)

  // End-time slots show duration labels relative to start
  const endSlots: (TimeSlot | EndTimeSlot)[] = startTime
    ? getEndTimeSlots(startTime.hours, startTime.minutes, 30)
    : ALL_SLOTS

  function handleDateChange(date: Date | null) {
    if (!date) {
      onStartsAtChange(null)
      onEndsAtChange(null)
      return
    }
    const base = dateOnly(date)

    if (startTime) {
      onStartsAtChange(setMinutes(setHours(base, startTime.hours), startTime.minutes))
    } else {
      onStartsAtChange(base)
    }
    if (endTime) {
      onEndsAtChange(setMinutes(setHours(base, endTime.hours), endTime.minutes))
    } else if (startTime) {
      // Auto-set end time to 1 hr after start
      const endMins = startTime.hours * 60 + startTime.minutes + DEFAULT_DURATION_MINUTES
      const eh = Math.min(Math.floor(endMins / 60), 23)
      const em = endMins % 60
      onEndsAtChange(setMinutes(setHours(base, eh), em))
    }
  }

  function handleStartTimeChange(time: { hours: number; minutes: number }) {
    const base = sessionDate ? dateOnly(sessionDate) : dateOnly(new Date())
    const newStart = setMinutes(setHours(base, time.hours), time.minutes)
    onStartsAtChange(newStart)

    // Compute previous duration to maintain, or default to 1 hr
    let durationMins = DEFAULT_DURATION_MINUTES
    if (startTime && endTime) {
      const prevDur =
        (endTime.hours * 60 + endTime.minutes) -
        (startTime.hours * 60 + startTime.minutes)
      if (prevDur > 0) durationMins = prevDur
    }

    const endMins = time.hours * 60 + time.minutes + durationMins
    if (endMins < 24 * 60) {
      const eh = Math.floor(endMins / 60)
      const em = endMins % 60
      onEndsAtChange(setMinutes(setHours(base, eh), em))
    } else {
      // Clamp to 11:30 PM
      onEndsAtChange(setMinutes(setHours(base, 23), 30))
    }
  }

  function handleEndTimeChange(time: { hours: number; minutes: number }) {
    const base = sessionDate ? dateOnly(sessionDate) : dateOnly(new Date())
    onEndsAtChange(setMinutes(setHours(base, time.hours), time.minutes))
  }

  return (
    <div className="space-y-4">
      <DatePicker
        label="Date"
        value={sessionDate}
        onChange={handleDateChange}
        disabled={disabled}
      />
      <div className="flex items-end gap-3">
        <div className="flex-1">
          <TimePicker
            label="Start Time"
            value={startTime}
            onChange={handleStartTimeChange}
            slots={ALL_SLOTS}
            disabled={disabled}
            placeholder="Start"
          />
        </div>
        <span
          className="pb-2 text-sm"
          style={{ color: 'var(--fgColor-muted)' }}
        >
          –
        </span>
        <div className="flex-1">
          <TimePicker
            label="End Time"
            value={endTime}
            onChange={handleEndTimeChange}
            slots={endSlots}
            disabled={disabled}
            placeholder="End"
          />
        </div>
      </div>
    </div>
  )
}
