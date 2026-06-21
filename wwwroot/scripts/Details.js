
    $(document).ready(function () {

        // ───────────────────────────────────────────────
        //  متغير عام لمعرفة إذا فيه تغييرات غير محفوظة
        // ───────────────────────────────────────────────
        window.isDirty = false;   // نستخدم window ليكون متاح في كل الدوال

    // ربط أحداث التغيير على الحقول داخل medical-history فقط
    $('#medical-history input, #medical-history textarea, #medical-history select').on('change input', function () {
        window.isDirty = true;
    });

    // ───────────────────────────────────────────────
    //  التنقل بين التبويبات
    // ───────────────────────────────────────────────
    $('.sidebar-link').on('click', function (e) {
        e.preventDefault();

    // إذا كان الرابط معطل → لا نفعل شيئًا
    if ($(this).hasClass('disabled-link')) {
            return;
        }

    // إذا كان هناك تغييرات غير محفوظة ونحن ننتقل من medical-history
    const currentActive = $('.content-section.active').attr('id');
    if (currentActive === 'medical-history' && window.isDirty) {
        showUnsavedWarning();
    return;
        }

    // إزالة الـ active من الكل
    $('.sidebar-link').removeClass('tab-active');
    $('.content-section').removeClass('active');

    // تفعيل التب الجديد
    $(this).addClass('tab-active');
    const target = $(this).data('target');
    const $targetSection = $('#' + target);

    if ($targetSection.length) {
        $targetSection.addClass('active');
        }

    // حفظ التب في الـ URL (بدون إعادة تحميل)
    const url = new URL(window.location);
    url.searchParams.set('tab', target);
    window.history.replaceState(null, '', url);
    });

    // ───────────────────────────────────────────────
    //  تفعيل التب النشط عند تحميل الصفحة
    // ───────────────────────────────────────────────
    function activateInitialTab() {
        const urlParams = new URLSearchParams(window.location.search);
    let initialTab = urlParams.get('tab') || 'medical-history';

    const $tabLink = $(`.sidebar-link[data-target="${initialTab}"]`);

    // إذا التب موجود وغير معطل → نفّذ النقر
    if ($tabLink.length && !$tabLink.hasClass('disabled-link')) {
        $tabLink.trigger('click');
        }
    // fallback → medical history
    else {
        $('.sidebar-link[data-target="medical-history"]').trigger('click');
        }
    }

    activateInitialTab();

    // ───────────────────────────────────────────────
    //  التحكم في قفل/فتح Dental History بناءً على الـ checkbox
    // ───────────────────────────────────────────────
    const $checkbox = $('#medicalHistoryCheckbox');
    const $dentalLink = $('#dental-history-link');

    function updateDentalHistoryState() {
        if ($checkbox.is(':checked')) {
        $('#medical-history-alert').fadeOut(300);
    $dentalLink.removeClass('disabled-link').css({
        'pointer-events': '',
    'opacity': '',
    'cursor': ''
            });
        } else {
        $('#medical-history-alert').fadeIn(300);
    $dentalLink.addClass('disabled-link').css({
        'pointer-events': 'none',
    'opacity': '0.4',
    'cursor': 'not-allowed'
            });
        }
    }

    $checkbox.on('change', updateDentalHistoryState);

    // تشغيل الحالة الأولية
    updateDentalHistoryState();

    // عند الضغط على زر Save → نعيد isDirty إلى false
    $('#saveBtn').on('click', function () {
        // نترك الـ submit يحصل طبيعيًا
        // لكن يمكننا إضافة تأخير صغير إذا أردت التأكد
        setTimeout(() => {
            window.isDirty = false;
        }, 300);
    });
});

    // ───────────────────────────────────────────────
    //  دالة التحذير من التغييرات غير المحفوظة
    // ───────────────────────────────────────────────
    function showUnsavedWarning() {
    if (document.getElementById('unsaved-warning')) return;

    const warning = document.createElement('div');
    warning.id = 'unsaved-warning';
    warning.innerHTML = `
    <div style="position:fixed; inset:0; background:rgba(0,0,0,0.4); z-index:99999; display:flex; align-items:center; justify-content:center;">
        <div style="background:white; border:2px solid #daa328; border-radius:10px; padding:24px 28px; box-shadow:0 8px 30px rgba(0,0,0,0.2); max-width:360px; width:90%; text-align:center;">
            <div style="font-size:36px; margin-bottom:10px;">⚠️</div>
            <div style="font-weight:700; font-size:15px; color:#252468; margin-bottom:8px;">Unsaved Changes</div>
            <div style="font-size:13px; color:#555; margin-bottom:20px;">
                You have unsaved changes in Medical History.<br>Please save before continuing.
            </div>
            <button onclick="this.closest('#unsaved-warning').remove()"
                style="background:#252468; color:white; border:none; padding:8px 28px; font-size:13px; font-weight:600; border-radius:5px; cursor:pointer;">
                OK
            </button>
        </div>
    </div>`;

    document.body.appendChild(warning);

    // إزالة تلقائية بعد 5 ثوانٍ
    setTimeout(() => {
        const w = document.getElementById('unsaved-warning');
    if (w) w.remove();
    }, 5000);
}

// ───────────────────────────────────────────────
//  باقي الدوال (Condition & Medication modals) 
//  تبقى كما هي إلا إذا أردت تعديلات إضافية
// ───────────────────────────────────────────────
// ... (ضع هنا دوال openConditionModal, saveConditions, saveMedications, إلخ)
