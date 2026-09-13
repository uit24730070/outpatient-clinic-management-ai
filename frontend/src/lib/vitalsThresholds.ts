// Ngưỡng tham khảo cơ bản cho sinh hiệu người lớn (Epic 18, VIS-04) — CHỈ để tô màu trực quan
// trên UI, KHÔNG phải cảnh báo lâm sàng chính thức (không phân biệt tuổi/giới, không thay thế
// đánh giá của nhân viên y tế). Ngưỡng cố định đơn giản, không cấu hình được.

export type VitalTone = 'default' | 'warning' | 'danger'

function rangeTone(
  value: number | null,
  danger: [number, number],
  warning: [number, number],
): VitalTone {
  if (value == null) return 'default'
  if (value < danger[0] || value > danger[1]) return 'danger'
  if (value < warning[0] || value > warning[1]) return 'warning'
  return 'default'
}

export function temperatureTone(c: number | null): VitalTone {
  return rangeTone(c, [35, 39], [36.1, 37.5])
}

export function pulseTone(bpm: number | null): VitalTone {
  return rangeTone(bpm, [50, 130], [60, 100])
}

export function spO2Tone(percent: number | null): VitalTone {
  if (percent == null) return 'default'
  if (percent < 90) return 'danger'
  if (percent < 95) return 'warning'
  return 'default'
}

export function respiratoryRateTone(bpm: number | null): VitalTone {
  return rangeTone(bpm, [8, 30], [12, 20])
}

export function bloodPressureTone(systolic: number | null, diastolic: number | null): VitalTone {
  const sysTone = rangeTone(systolic, [80, 179], [90, 139])
  const diaTone = rangeTone(diastolic, [50, 109], [60, 89])
  if (sysTone === 'danger' || diaTone === 'danger') return 'danger'
  if (sysTone === 'warning' || diaTone === 'warning') return 'warning'
  return 'default'
}

/** Tone nặng nhất trong danh sách — dùng để tô cả dòng lịch sử hoặc badge tổng hợp. */
export function worstTone(tones: VitalTone[]): VitalTone {
  if (tones.includes('danger')) return 'danger'
  if (tones.includes('warning')) return 'warning'
  return 'default'
}
