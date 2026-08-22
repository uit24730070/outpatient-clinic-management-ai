import { useNavigate, useParams } from 'react-router-dom'
import { EncounterForm } from '../components/EncounterForm'

/**
 * Route lập phiếu khám cho một lịch (đường dẫn cũ /appointments/:appointmentId/encounter).
 * Chỉ là vỏ bọc quanh <EncounterForm/> — thân đã tách để nhúng được trong màn khám đa tab (CLS-06).
 */
export default function EncounterFormPage() {
  const { appointmentId } = useParams<{ appointmentId: string }>()
  const navigate = useNavigate()

  if (!appointmentId) return null

  return (
    <EncounterForm
      appointmentId={appointmentId}
      onBack={() => navigate('/appointments')}
      onCompleted={() => navigate('/appointments')}
    />
  )
}
