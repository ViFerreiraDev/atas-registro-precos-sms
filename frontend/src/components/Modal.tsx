import { X, AlertCircle, CheckCircle2, AlertTriangle, Info } from 'lucide-react';
import { useEffect } from 'react';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  message: string;
  type?: 'success' | 'error' | 'warning' | 'info';
  confirmText?: string;
  cancelText?: string;
  onConfirm?: () => void;
  showCancel?: boolean;
}

export function Modal({
  isOpen,
  onClose,
  title,
  message,
  type = 'info',
  confirmText = 'OK',
  cancelText = 'Cancelar',
  onConfirm,
  showCancel = false
}: ModalProps) {
  // Fechar com ESC
  useEffect(() => {
    const handleEsc = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      document.addEventListener('keydown', handleEsc);
      document.body.style.overflow = 'hidden';
    }
    return () => {
      document.removeEventListener('keydown', handleEsc);
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const icons = {
    success: <CheckCircle2 className="text-green-500" size={48} />,
    error: <AlertCircle className="text-red-500" size={48} />,
    warning: <AlertTriangle className="text-yellow-500" size={48} />,
    info: <Info className="text-blue-500" size={48} />
  };

  const buttonColors = {
    success: 'bg-green-500 hover:bg-green-600',
    error: 'bg-red-500 hover:bg-red-600',
    warning: 'bg-yellow-500 hover:bg-yellow-600',
    info: 'bg-[#00508C] hover:bg-[#003d6b]'
  };

  const handleConfirm = () => {
    if (onConfirm) {
      onConfirm();
    }
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/50 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Modal */}
      <div className="relative bg-white rounded-2xl shadow-xl w-full max-w-sm overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Close button */}
        <button
          onClick={onClose}
          className="absolute top-3 right-3 p-1 rounded-full hover:bg-gray-100 transition-colors"
        >
          <X size={20} className="text-gray-400" />
        </button>

        {/* Content */}
        <div className="p-6 pt-8 text-center">
          <div className="flex justify-center mb-4">
            {icons[type]}
          </div>

          <h3 className="text-lg font-semibold text-gray-900 mb-2">
            {title}
          </h3>

          <p className="text-gray-600 text-sm mb-6">
            {message}
          </p>

          {/* Buttons */}
          <div className={`flex gap-3 ${showCancel ? 'justify-center' : ''}`}>
            {showCancel && (
              <button
                onClick={onClose}
                className="flex-1 px-4 py-3 bg-gray-100 text-gray-700 rounded-xl font-medium hover:bg-gray-200 transition-colors"
              >
                {cancelText}
              </button>
            )}
            <button
              onClick={handleConfirm}
              className={`flex-1 px-4 py-3 text-white rounded-xl font-medium transition-colors ${buttonColors[type]}`}
            >
              {confirmText}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

// Hook para usar o modal mais facilmente
import { useState, useCallback } from 'react';

interface ModalState {
  isOpen: boolean;
  title: string;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  onConfirm?: () => void;
  showCancel: boolean;
  confirmText: string;
  cancelText: string;
}

export function useModal() {
  const [modalState, setModalState] = useState<ModalState>({
    isOpen: false,
    title: '',
    message: '',
    type: 'info',
    showCancel: false,
    confirmText: 'OK',
    cancelText: 'Cancelar'
  });

  const showModal = useCallback((options: {
    title: string;
    message: string;
    type?: 'success' | 'error' | 'warning' | 'info';
    onConfirm?: () => void;
    showCancel?: boolean;
    confirmText?: string;
    cancelText?: string;
  }) => {
    setModalState({
      isOpen: true,
      title: options.title,
      message: options.message,
      type: options.type || 'info',
      onConfirm: options.onConfirm,
      showCancel: options.showCancel || false,
      confirmText: options.confirmText || 'OK',
      cancelText: options.cancelText || 'Cancelar'
    });
  }, []);

  const hideModal = useCallback(() => {
    setModalState(prev => ({ ...prev, isOpen: false }));
  }, []);

  const showSuccess = useCallback((title: string, message: string) => {
    showModal({ title, message, type: 'success' });
  }, [showModal]);

  const showError = useCallback((title: string, message: string) => {
    showModal({ title, message, type: 'error' });
  }, [showModal]);

  const showWarning = useCallback((title: string, message: string) => {
    showModal({ title, message, type: 'warning' });
  }, [showModal]);

  const showConfirm = useCallback((
    title: string,
    message: string,
    onConfirm: () => void,
    type: 'warning' | 'error' | 'info' = 'warning'
  ) => {
    showModal({
      title,
      message,
      type,
      onConfirm,
      showCancel: true,
      confirmText: 'Confirmar'
    });
  }, [showModal]);

  return {
    modalState,
    showModal,
    hideModal,
    showSuccess,
    showError,
    showWarning,
    showConfirm,
    Modal: () => (
      <Modal
        isOpen={modalState.isOpen}
        onClose={hideModal}
        title={modalState.title}
        message={modalState.message}
        type={modalState.type}
        onConfirm={modalState.onConfirm}
        showCancel={modalState.showCancel}
        confirmText={modalState.confirmText}
        cancelText={modalState.cancelText}
      />
    )
  };
}
