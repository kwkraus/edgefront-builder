export interface TimeSlot {
  hours: number
  minutes: number
  /** "HH:mm" 24-hour format */
  value: string
  /** "h:mm AM/PM" 12-hour display label */
  label: string
}

export interface EndTimeSlot extends TimeSlot {
  /** Human-readable duration from start, e.g. "30 mins", "1 hr" */
  durationLabel: string
}

export function timeSlotToMinutes(slot: { hours: number; minutes: number }): number {
  return slot.hours * 60 + slot.minutes
}

function format12Hour(hours: number, minutes: number): string {
  const period = hours >= 12 ? 'PM' : 'AM'
  const h = hours % 12 || 12
  const m = String(minutes).padStart(2, '0')
  return `${h}:${m} ${period}`
}

export function generateTimeSlots(intervalMinutes: number = 30): TimeSlot[] {
  const slots: TimeSlot[] = []
  for (let totalMins = 0; totalMins < 24 * 60; totalMins += intervalMinutes) {
    const hours = Math.floor(totalMins / 60)
    const minutes = totalMins % 60
    slots.push({
      hours,
      minutes,
      value: `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`,
      label: format12Hour(hours, minutes),
    })
  }
  return slots
}

export function formatDuration(totalMinutes: number): string {
  if (totalMinutes <= 0) return '0 mins'
  if (totalMinutes < 60) return `${totalMinutes} mins`
  const hrs = totalMinutes / 60
  if (Number.isInteger(hrs)) {
    return hrs === 1 ? '1 hr' : `${hrs} hrs`
  }
  return `${hrs} hrs`
}

export function getEndTimeSlots(
  startHours: number,
  startMinutes: number,
  intervalMinutes: number = 30,
): EndTimeSlot[] {
  const startTotal = startHours * 60 + startMinutes
  const allSlots = generateTimeSlots(intervalMinutes)
  return allSlots
    .filter((s) => timeSlotToMinutes(s) > startTotal)
    .map((s) => ({
      ...s,
      durationLabel: `${s.label} (${formatDuration(timeSlotToMinutes(s) - startTotal)})`,
    }))
}

export function nearestSlotIndex(
  hours: number,
  minutes: number,
  slots: TimeSlot[],
): number {
  const target = hours * 60 + minutes
  let bestIdx = 0
  let bestDiff = Infinity
  for (let i = 0; i < slots.length; i++) {
    const diff = Math.abs(timeSlotToMinutes(slots[i]) - target)
    if (diff < bestDiff) {
      bestDiff = diff
      bestIdx = i
    }
  }
  return bestIdx
}
