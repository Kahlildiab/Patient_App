$(document).ready(function () {
    window.isDirty = false;

    $('#medical-history input, #medical-history textarea, #medical-history select')
        .on('change input', function () { window.isDirty = true; });

    $('.sidebar-link').on('click', function (e) {
        e.preventDefault();
        if ($(this).hasClass('disabled-link')) return;
        const currentActive = $('.content-section.active').attr('id');
        if (currentActive === 'medical-history' && window.isDirty) { showUnsavedWarning(); return; }
        $('.sidebar-link').removeClass('tab-active');
        $('.content-section').removeClass('active');
        $(this).addClass('tab-active');
        const target = $(this).data('target');
        $('#' + target).addClass('active');
        $('#content-section-title').text($(this).find('span').text().toUpperCase());
        const url = new URL(window.location);
        url.searchParams.set('tab', target);
        window.history.replaceState(null, '', url);
    });

    function activateInitialTab() {
        const urlParams = new URLSearchParams(window.location.search);
        let initialTab = urlParams.get('tab') || 'medical-history';
        const $tabLink = $(`.sidebar-link[data-target="${initialTab}"]`);
        if ($tabLink.length && !$tabLink.hasClass('disabled-link')) {
            $tabLink.trigger('click');
        } else {
            $('.sidebar-link[data-target="medical-history"]').trigger('click');
        }
        const lbl = $(`.sidebar-link[data-target="${initialTab}"]`).find('span').text().toUpperCase();
        if (lbl) $('#content-section-title').text(lbl);
    }
    activateInitialTab();

    const $checkbox = $('#medicalHistoryCheckbox');
    function updateDentalHistoryState() {
        const $allLinks = $('.sidebar-link').not('[data-target="medical-history"]');
        if ($checkbox.is(':checked')) {
            $('#medical-history-alert').fadeOut(300);
            $allLinks.removeClass('disabled-link').css({ 'pointer-events': '', 'opacity': '', 'cursor': '' });
        } else {
            $('#medical-history-alert').fadeIn(300);
            $allLinks.addClass('disabled-link').css({ 'pointer-events': 'none', 'opacity': '0.4', 'cursor': 'not-allowed' });
        }
    }
    $checkbox.on('change', updateDentalHistoryState);
    updateDentalHistoryState();

    $('#saveBtn').on('click', function (e) {
        // ✅ Allergy validation
        const isYesAllergy = document.querySelector('input[name="Allergies"][value="true"]')?.checked;
        const allergyTa = document.querySelector('textarea[name="AllergyNotes"]');
        const allergyError = document.getElementById('allergyNotesError');
        if (isYesAllergy && !allergyTa?.value?.trim()) {
            e.preventDefault();
            if (allergyError) allergyError.style.display = 'block';
            if (allergyTa) { allergyTa.style.border = '1.5px solid #c0392b'; allergyTa.focus(); }
            return;
        }
        if (allergyError) allergyError.style.display = 'none';
        if (allergyTa) allergyTa.style.border = '1px solid #ddd';

        // ✅ Hospitalization validation
        const isYes = document.querySelector('input[name="RecentHospitalization"][value="true"]')?.checked;
        const ta = document.getElementById('hospitalizationNotesText');
        const errorEl = document.getElementById('hospitalizationNotesError');
        if (isYes && !ta?.value?.trim()) {
            e.preventDefault();
            if (errorEl) errorEl.style.display = 'block';
            if (ta) { ta.style.border = '1.5px solid #c0392b'; ta.focus(); }
            return;
        }
        if (errorEl) errorEl.style.display = 'none';
        if (ta) ta.style.border = '1px solid #ddd';
        setTimeout(() => { window.isDirty = false; }, 300);
    });

    $(document).on('click', '.edit-condition-btn', function () {
        const btn = $(this);
        document.getElementById('editConditionID').value = btn.data('id');
        document.getElementById('editConditionName').value = btn.data('name');
        document.getElementById('editConditionYear').value = btn.data('year') || '';
        document.getElementById('editConditionActive').checked = btn.data('active') === 'true';
        document.getElementById('editConditionFlagged').checked = btn.data('flagged') === 'true';
        document.getElementById('editConditionNotes').value = btn.data('notes') || '';
        document.getElementById('editConditionModal').style.display = 'flex';
    });

    $(document).on('click', '.edit-med-btn', function () {
        const btn = $(this);
        document.getElementById('editMedID').value = btn.data('id');
        document.getElementById('editMedDrugName').value = btn.data('drug');
        document.getElementById('editMedDose').value = btn.data('dose');
        document.getElementById('editMedFrequency').value = btn.data('frequency');
        document.getElementById('editMedDuration').value = btn.data('duration');
        document.getElementById('editMedFlagType').value = btn.data('flag') || '';
        document.getElementById('editMedicationModal').style.display = 'flex';
    });

    openConditionModal = function () {
        document.getElementById('conditions-modal-list').innerHTML = buildConditionRowHTML(0);
        conditionRowIndex = 1;
        document.getElementById('conditionModal').style.display = 'flex';
    };
    openMedicationModal = function () {
        document.getElementById('medications-modal-list').innerHTML = buildMedicationRowHTML(0);
        medicationRowIndex = 1;
        document.getElementById('medicationModal').style.display = 'flex';
    };
});

// ═══════════════════════════════════════════════
//  Unsaved Warning
// ═══════════════════════════════════════════════
function showUnsavedWarning() {
    if (document.getElementById('unsaved-warning')) return;
    const w = document.createElement('div');
    w.id = 'unsaved-warning';
    w.innerHTML = `
        <div style="position:fixed; inset:0; background:rgba(0,0,0,0.4); z-index:99999; display:flex; align-items:center; justify-content:center;">
            <div style="background:white; border:2px solid #daa328; border-radius:10px; padding:24px 28px; max-width:360px; width:90%; text-align:center;">
                <div style="font-size:36px; margin-bottom:10px;">⚠️</div>
                <div style="font-weight:700; font-size:15px; color:#252468; margin-bottom:8px;">Unsaved Changes</div>
                <div style="font-size:13px; color:#555; margin-bottom:20px;">You have unsaved changes in Medical History.<br>Please save before continuing.</div>
                <button onclick="document.getElementById('unsaved-warning').remove()"
                    style="background:#252468; color:white; border:none; padding:8px 28px; font-size:13px; font-weight:600; border-radius:5px; cursor:pointer;">OK</button>
            </div>
        </div>`;
    document.body.appendChild(w);
    setTimeout(() => { const x = document.getElementById('unsaved-warning'); if (x) x.remove(); }, 5000);
}

function toggleAllergyNotes(show) {
    const box = document.getElementById('allergyNotesBox');
    if (box) box.style.display = show ? 'block' : 'none';
    if (!show) { const ta = box?.querySelector('textarea'); if (ta) ta.value = ''; }

    let errorEl = document.getElementById('allergyNotesError');
    if (!errorEl) {
        errorEl = document.createElement('div');
        errorEl.id = 'allergyNotesError';
        errorEl.style.cssText = 'display:none; color:#c0392b; font-size:12px; margin-top:4px;';
        errorEl.innerHTML = '⚠️ Please describe the allergy.';
        box?.appendChild(errorEl);
    }
    if (!show) errorEl.style.display = 'none';
}

function toggleHospitalizationNotes(show) {
    const box = document.getElementById('hospitalizationNotesBox');
    const errorEl = document.getElementById('hospitalizationNotesError');
    const ta = document.getElementById('hospitalizationNotesText');
    if (box) box.style.display = show ? 'block' : 'none';
    if (!show) {
        if (ta) { ta.value = ''; ta.style.border = '1px solid #ddd'; }
        if (errorEl) errorEl.style.display = 'none';
    }
}

// ═══════════════════════════════════════════════
//  Info Bar — Red Flags updater
// ═══════════════════════════════════════════════
function updateRedFlagsInfoBar() {
    const flaggedNames = [];
    document.querySelectorAll('#conditions-tbody tr').forEach(row => {
        const nameCell = row.cells[0];
        const flagCell = row.cells[3];
        if (flagCell && flagCell.querySelector('span')?.textContent.includes('Yes')) {
            flaggedNames.push(nameCell?.textContent.trim());
        }
    });

    const infoBarRedFlags = document.getElementById('info-bar-red-flags');
    const infoBarRedFlagsWrapper = document.getElementById('info-bar-red-flags-wrapper');

    if (flaggedNames.length > 0) {
        if (infoBarRedFlagsWrapper) infoBarRedFlagsWrapper.style.display = 'flex';
        if (infoBarRedFlags) infoBarRedFlags.textContent = flaggedNames.join(', ');
    } else {
        if (infoBarRedFlagsWrapper) infoBarRedFlagsWrapper.style.display = 'none';
    }
}

// ═══════════════════════════════════════════════
//  CONDITION — buildRowHTML
// ═══════════════════════════════════════════════
let conditionRowIndex = 1;

function buildConditionRowHTML(i, removable = false) {
    return `
        <div class="condition-row-modal" style="${removable ? 'border-top:1px dashed #ddd; padding-top:12px; margin-top:12px;' : ''}">
            ${removable ? `<div style="display:flex; justify-content:flex-end; margin-bottom:6px;">
                <button type="button" onclick="this.closest('.condition-row-modal').remove()"
                    style="background:none; border:none; color:#c0392b; cursor:pointer; font-size:13px;">✕ Remove</button>
            </div>` : ''}
            <div style="display:grid; grid-template-columns:1fr 1fr; gap:14px; margin-bottom:12px;">
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; margin-bottom:5px; display:block;">Condition</label>
                    <input type="text" name="Conditions[${i}].ConditionName"
                        style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;"
                        placeholder="e.g. Hypertension" />
                </div>
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; margin-bottom:5px; display:block;">Year of Diagnosis</label>
                    <input type="number" name="Conditions[${i}].YearOfDiagnosis" min="1900" max="2100"
                        maxlength="4" oninput="if(this.value.length > 4) this.value = this.value.slice(0, 4);"
                        style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;"
                        placeholder="e.g. 2020" />
                    <label style="display:flex; align-items:center; gap:6px; font-size:12px; margin-top:8px; cursor:pointer; color:#c0392b; font-weight:600;">
                        <input type="checkbox" name="Conditions[${i}].IsFlagged" value="true" style="accent-color:#c0392b;" />
                        🚩 Red Flag
                    </label>
                </div>
            </div>
            <div style="display:flex; align-items:center; gap:8px; margin-bottom:12px; font-size:13px;">
                <input type="checkbox" name="Conditions[${i}].IsCurrentlyActive" value="true" checked style="accent-color:#252468;" /> Currently Active
            </div>
            <div>
                <label style="font-size:12px; font-weight:600; color:#555; margin-bottom:5px; display:block;">Notes</label>
                <textarea name="Conditions[${i}].Notes"
                    style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box; resize:vertical; min-height:70px;"
                    placeholder="Any additional notes..."></textarea>
            </div>
        </div>`;
}

function addModalRow() {
    const list = document.getElementById('conditions-modal-list');
    const div = document.createElement('div');
    div.innerHTML = buildConditionRowHTML(conditionRowIndex++, true);
    list.appendChild(div.firstElementChild);
}

function saveConditions() {
    const form = document.getElementById('conditionForm');
    const data = new FormData(form);
    const saveBtn = document.querySelector('#conditionModal button[onclick="saveConditions()"]');
    if (!data.get('Conditions[0].ConditionName')?.trim()) {
        showModalAlert('conditionModal', 'Please enter at least one condition name.'); return;
    }
    saveBtn.disabled = true; saveBtn.textContent = 'Saving...';
    fetch('/Conditions/Create', { method: 'POST', body: data })
        .then(res => res.json())
        .then(result => {
            if (result.success) {
                closeConditionModal();
                appendConditionsToTable(result.conditions);
                window.isDirty = true;
                showToast('Condition saved successfully.', 'success');
            } else { showModalAlert('conditionModal', result.message || 'Error saving conditions.'); }
        })
        .catch(() => showModalAlert('conditionModal', 'Server error. Please try again.'))
        .finally(() => { saveBtn.disabled = false; saveBtn.textContent = 'Save Conditions'; });
}

function appendConditionsToTable(conditions) {
    const noMsg = document.getElementById('no-conditions-msg');
    let table = document.getElementById('conditions-table');
    let tbody = document.getElementById('conditions-tbody');
    if (noMsg) noMsg.style.display = 'none';
    if (table) table.style.display = '';
    if (!tbody) tbody = document.querySelector('#conditions-display tbody');

    conditions.forEach(c => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="padding:6px 10px; font-weight:600; color:#252468;">${c.conditionName}</td>
            <td style="padding:6px 10px;">${c.yearOfDiagnosis ?? '---'}</td>
            <td style="padding:6px 10px;">${c.notes ?? '---'}</td>
            <td style="padding:6px 10px;">${c.isFlagged ? '<span style="color:#c0392b; font-weight:700;">🚩 Yes</span>' : '<span style="color:#aaa; font-size:11px;">No</span>'}</td>
            <td style="padding:6px 10px; text-align:center; white-space:nowrap;">
                <button type="button" class="edit-condition-btn"
                        data-id="${c.conditionID}"
                        data-name="${(c.conditionName || '').replace(/"/g, '&quot;')}"
                        data-year="${c.yearOfDiagnosis ?? ''}"
                        data-active="${c.isCurrentlyActive ?? false}"
                        data-flagged="${c.isFlagged ?? false}"
                        data-notes="${(c.notes || '').replace(/"/g, '&quot;')}"
                        style="background:none; border:none; color:#252468; cursor:pointer; margin-right:8px;" title="Edit">
                    <i class="fa-solid fa-pen-to-square"></i>
                </button>
                <button type="button" onclick="deleteCondition(this, ${c.conditionID})"
                        style="background:none; border:none; color:#c0392b; cursor:pointer;" title="Delete">
                    <i class="fa-solid fa-trash"></i>
                </button>
            </td>`;
        tbody.appendChild(tr);
    });

    updateRedFlagsInfoBar();
}

function closeConditionModal() { document.getElementById('conditionModal').style.display = 'none'; }
function closeEditConditionModal() { document.getElementById('editConditionModal').style.display = 'none'; }

function deleteCondition(btn, conditionId) {
    if (!confirm('Are you sure you want to delete this condition?')) return;
    const row = btn.closest('tr');
    fetch('/Conditions/DeleteAjax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `id=${conditionId}&__RequestVerificationToken=${encodeURIComponent(getAntiForgeryToken())}`
    })
        .then(res => res.json())
        .then(result => {
            if (result.success) {
                row.remove();
                const tbody = document.getElementById('conditions-tbody');
                if (tbody && tbody.querySelectorAll('tr').length === 0) {
                    const noMsg = document.getElementById('no-conditions-msg');
                    const table = document.getElementById('conditions-table');
                    if (noMsg) noMsg.style.display = '';
                    if (table) table.style.display = 'none';
                }
                updateRedFlagsInfoBar();
                showToast('Condition deleted successfully.', 'success');
            } else { showToast('Error deleting condition.', 'error'); }
        })
        .catch(() => showToast('Server error.', 'error'));
}

function submitEditCondition() {
    const form = document.getElementById('editConditionForm');
    const data = new FormData(form);
    const saveBtn = document.getElementById('editConditionSaveBtn');
    saveBtn.disabled = true; saveBtn.textContent = 'Saving...';
    fetch('/Conditions/EditAjax', { method: 'POST', body: data })
        .then(res => res.json())
        .then(result => {
            if (result.success) {
                closeEditConditionModal();
                showToast('Condition updated successfully.', 'success');
                setTimeout(() => location.reload(), 1200);
            } else { showToast(result.message || 'Error updating condition.', 'error'); }
        })
        .catch(() => showToast('Server error.', 'error'))
        .finally(() => { saveBtn.disabled = false; saveBtn.textContent = 'Save Changes'; });
}

// ═══════════════════════════════════════════════
//  MEDICATION
// ═══════════════════════════════════════════════
let medicationRowIndex = 1;

function buildMedicationRowHTML(i, removable = false) {
    return `
        <div class="medication-row-modal" style="${removable ? 'border-top:1px dashed #ddd; padding-top:12px; margin-top:12px;' : ''}">
            ${removable ? `<div style="display:flex; justify-content:flex-end; margin-bottom:6px;">
                <button type="button" onclick="this.closest('.medication-row-modal').remove()"
                    style="background:none; border:none; color:#c0392b; cursor:pointer; font-size:13px;">✕ Remove</button>
            </div>` : ''}
            <div style="margin-bottom:12px;">
                <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">Flag Type</label>
                <select name="Medications[${i}].FlagType"
                    style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;">
                    <option value="">-- None --</option>
                    <option value="Anticoagulant">Anticoagulant</option>
                    <option value="Bisphosphonates">Bisphosphonates / Antiresorptive</option>
                    <option value="Steroids">Steroids (Systemic)</option>
                    <option value="Immunosuppressants">Immunosuppressants</option>
                </select>
            </div>
            <div style="display:grid; grid-template-columns:1fr 1fr; gap:14px; margin-bottom:12px;">
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">Drug Name</label>
                    <input type="text" name="Medications[${i}].DrugName"
                        style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;" placeholder="e.g. Aspirin" />
                </div>
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">Dose</label>
                    <input type="text" name="Medications[${i}].Dose"
                        style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;" placeholder="e.g. 100mg" />
                </div>
            </div>
            <div style="display:grid; grid-template-columns:1fr 1fr; gap:14px; margin-bottom:12px;">
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">Frequency</label>
                    <input type="text" name="Medications[${i}].Frequency"
                        style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;" placeholder="e.g. Once daily" />
                </div>
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">Duration</label>
                    <input type="text" name="Medications[${i}].Duration"
                        style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;" placeholder="e.g. 3 months" />
                </div>
            </div>
        </div>`;
}

function addMedicationRow() {
    const list = document.getElementById('medications-modal-list');
    const div = document.createElement('div');
    div.innerHTML = buildMedicationRowHTML(medicationRowIndex++, true);
    list.appendChild(div.firstElementChild);
}

function saveMedications() {
    const form = document.getElementById('medicationForm');
    const data = new FormData(form);
    const saveBtn = document.querySelector('#medicationModal button[onclick="saveMedications()"]');
    if (!data.get('Medications[0].DrugName')?.trim()) {
        showModalAlert('medicationModal', 'Please enter at least one drug name.'); return;
    }
    saveBtn.disabled = true; saveBtn.textContent = 'Saving...';
    fetch('/Medications/CreateAjax', { method: 'POST', body: data })
        .then(res => res.json())
        .then(result => {
            if (result.success) {
                closeMedicationModal();
                appendMedicationsToTable(result.medications);
                window.isDirty = true;
                showToast('Medication saved successfully.', 'success');
            } else { showModalAlert('medicationModal', result.message || 'Error saving medications.'); }
        })
        .catch(() => showModalAlert('medicationModal', 'Server error. Please try again.'))
        .finally(() => { saveBtn.disabled = false; saveBtn.textContent = 'Save Medications'; });
}

function appendMedicationsToTable(medications) {
    const noMsg = document.getElementById('no-medications-msg');
    let table = document.getElementById('medications-table');
    let tbody = document.getElementById('medications-tbody');
    if (noMsg) noMsg.style.display = 'none';
    if (table) table.style.display = '';
    if (!tbody) tbody = document.querySelector('#medications-display tbody');

    const flagMap = {
        'Anticoagulant': '<span class="flag-badge flag-anticoagulant">Anticoagulant</span>',
        'Bisphosphonates': '<span class="flag-badge flag-bisphosphonates">Bisphosphonates</span>',
        'Steroids': '<span class="flag-badge flag-steroids">Steroids</span>',
        'Immunosuppressants': '<span class="flag-badge flag-immunosuppressants">Immunosuppressants</span>'
    };

    medications.forEach(m => {
        const flagHtml = m.flagType && flagMap[m.flagType]
            ? flagMap[m.flagType]
            : '<span style="color:#aaa; font-size:11px;">---</span>';

        const tr = document.createElement('tr');
        tr.style.borderBottom = '1px solid #eee';
        tr.innerHTML = `
            <td style="padding:6px 10px;">${m.drugName}</td>
            <td style="padding:6px 10px;">${m.dose ?? '---'}</td>
            <td style="padding:6px 10px;">${m.frequency ?? '---'}</td>
            <td style="padding:6px 10px;">${m.duration ?? '---'}</td>
            <td style="padding:6px 10px;">${flagHtml}</td>
            <td style="padding:6px 10px; text-align:center; white-space:nowrap;">
                <button type="button" class="edit-med-btn"
                        data-id="${m.medicationID}"
                        data-drug="${(m.drugName || '').replace(/"/g, '&quot;')}"
                        data-dose="${(m.dose || '').replace(/"/g, '&quot;')}"
                        data-frequency="${(m.frequency || '').replace(/"/g, '&quot;')}"
                        data-duration="${(m.duration || '').replace(/"/g, '&quot;')}"
                        data-flag="${m.flagType || ''}"
                        style="background:none; border:none; color:#252468; cursor:pointer; margin-right:8px;" title="Edit">
                    <i class="fa-solid fa-pen-to-square"></i>
                </button>
                <button type="button" onclick="deleteMedication(this, ${m.medicationID})"
                        style="background:none; border:none; color:#c0392b; cursor:pointer;" title="Delete">
                    <i class="fa-solid fa-trash"></i>
                </button>
            </td>`;
        tbody.appendChild(tr);
    });
}

function closeMedicationModal() { document.getElementById('medicationModal').style.display = 'none'; }
function closeEditMedicationModal() { document.getElementById('editMedicationModal').style.display = 'none'; }

function deleteMedication(btn, medicationId) {
    if (!confirm('Are you sure you want to delete this medication?')) return;
    const row = btn.closest('tr');
    fetch('/Medications/DeleteAjax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `id=${medicationId}&__RequestVerificationToken=${encodeURIComponent(getAntiForgeryToken())}`
    })
        .then(res => res.json())
        .then(result => {
            if (result.success) {
                row.remove();
                const tbody = document.getElementById('medications-tbody');
                if (tbody && tbody.querySelectorAll('tr').length === 0) {
                    const noMsg = document.getElementById('no-medications-msg');
                    const table = document.getElementById('medications-table');
                    if (noMsg) noMsg.style.display = '';
                    if (table) table.style.display = 'none';
                }
                showToast('Medication deleted successfully.', 'success');
            } else { showToast('Error deleting medication.', 'error'); }
        })
        .catch(() => showToast('Server error.', 'error'));
}

function submitEditMedication() {
    const form = document.getElementById('editMedicationForm');
    const data = new FormData(form);
    const saveBtn = document.getElementById('editMedSaveBtn');
    saveBtn.disabled = true; saveBtn.textContent = 'Saving...';
    fetch('/Medications/EditAjax', { method: 'POST', body: data })
        .then(res => res.json())
        .then(result => {
            if (result.success) {
                closeEditMedicationModal();
                showToast('Medication updated successfully.', 'success');
                setTimeout(() => location.reload(), 1200);
            } else { showToast(result.message || 'Error updating medication.', 'error'); }
        })
        .catch(() => showToast('Server error.', 'error'))
        .finally(() => { saveBtn.disabled = false; saveBtn.textContent = 'Save Changes'; });
}

// ═══════════════════════════════════════════════
//  Toast & Helpers
// ═══════════════════════════════════════════════
function showToast(message, type = 'success') {
    const existing = document.getElementById('toast-notification');
    if (existing) existing.remove();
    const toast = document.createElement('div');
    toast.id = 'toast-notification';
    toast.style.cssText = `position:fixed; bottom:30px; right:30px; background:${type === 'success' ? '#252468' : '#c0392b'}; color:white; padding:14px 22px; border-radius:8px; font-size:13px; font-weight:600; z-index:999999; display:flex; align-items:center; gap:10px; box-shadow:0 4px 20px rgba(0,0,0,0.3);`;
    toast.innerHTML = `${type === 'success' ? '✅' : '❌'} ${message}`;
    document.body.appendChild(toast);
    setTimeout(() => {
        toast.style.transition = 'opacity 0.4s';
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 400);
    }, 3000);
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

function showModalAlert(modalId, message) {
    const modal = document.getElementById(modalId);
    let alertEl = modal.querySelector('.modal-alert');
    if (!alertEl) {
        alertEl = document.createElement('div');
        alertEl.className = 'modal-alert';
        alertEl.style.cssText = `background:#fff3cd; border:1px solid #daa328; border-radius:5px; padding:10px 14px; font-size:13px; color:#856404; margin-bottom:12px; display:flex; align-items:center; gap:8px;`;
        modal.querySelector('form').insertAdjacentElement('afterbegin', alertEl);
    }
    alertEl.innerHTML = `⚠️ ${message}`;
    setTimeout(() => alertEl.remove(), 4000);
}