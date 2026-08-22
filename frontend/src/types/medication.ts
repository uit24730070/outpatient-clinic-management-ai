// Kiểu dữ liệu miền Kho thuốc (thuốc, lô, phiếu nhập), khớp với API backend.

export interface Medication {
  id: string
  code: string
  name: string
  activeIngredient: string
  unit: string
  reorderLevel: number
  description: string | null
  /** Giá bán một đơn vị (VND), dùng tính tiền thuốc trên hoá đơn (BILL-02). */
  salePrice: number
  /** Tồn tổng = tổng tồn các lô chưa xoá (tính phía server). */
  stockOnHand: number
  createdAt: string
  updatedAt: string | null
}

export interface MedicationFormValues {
  name: string
  activeIngredient: string
  unit: string
  reorderLevel: number
  description: string | null
  salePrice: number
}

export interface MedicationBatch {
  id: string
  medicationId: string
  batchNumber: string
  /** Hạn dùng dạng "yyyy-MM-dd" (DateOnly). */
  expiryDate: string
  quantityOnHand: number
  createdAt: string
  updatedAt: string | null
}

export interface StockReceiptItem {
  medicationId: string
  medicationName: string | null
  batchNumber: string
  expiryDate: string
  quantity: number
  unitCost: number | null
}

export interface StockReceipt {
  id: string
  code: string
  supplierName: string
  receivedAt: string
  note: string | null
  items: StockReceiptItem[]
  createdAt: string
  updatedAt: string | null
}

/** Một dòng nhập trong form (khớp StockReceiptItemRequest phía backend). */
export interface StockReceiptItemInput {
  medicationId: string
  batchNumber: string
  expiryDate: string
  quantity: number
  unitCost: number | null
}

export interface StockReceiptFormValues {
  supplierName: string
  receivedAt: string
  note: string | null
  items: StockReceiptItemInput[]
}

// ── Cảnh báo kho (PH-08) ──────────────────────────────────────────────

export interface LowStockAlert {
  medicationId: string
  code: string
  name: string
  unit: string
  stockOnHand: number
  reorderLevel: number
}

export interface ExpiringBatchAlert {
  batchId: string
  medicationId: string
  medicationCode: string
  medicationName: string
  batchNumber: string
  expiryDate: string
  quantityOnHand: number
  isExpired: boolean
}

export interface PharmacyAlerts {
  lowStock: LowStockAlert[]
  expiringBatches: ExpiringBatchAlert[]
  expiringInDays: number
}
