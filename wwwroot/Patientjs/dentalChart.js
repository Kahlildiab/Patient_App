//// PATIENT_ID و CSRF_TOKEN يتم تعريفهم في Photos.cshtml قبل تحميل هذا الملف

//const _svgPerson = `<svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="#555" stroke-width="2.5">
//    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>`;
//const _svgCal = `<svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="#555" stroke-width="2.5">
//    <rect x="3" y="4" width="18" height="18" rx="2"/>
//    <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/>
//    <line x1="3" y1="10" x2="21" y2="10"/></svg>`;
//const _svgFile = `<svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="#555" stroke-width="2.5">
//    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
//    <polyline points="14 2 14 8 20 8"/></svg>`;
//const _svgInfo = `<svg width="8" height="8" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
//    <circle cx="12" cy="12" r="10"/>
//    <line x1="12" y1="8" x2="12" y2="12"/>
//    <line x1="12" y1="16" x2="12.01" y2="16"/></svg>`;

//function _buildInfo(nameIcon, nameTxt, dateTxt, sourceLabel, notes = '') {
//    return `
//<div class="ph-photo-info">
//    <div class="ph-info-row">
//        <div class="ph-icon-sq">${nameIcon}</div>
//        <span class="ph-info-section-name">${nameTxt}</span>
//    </div>
//    <div class="ph-info-divider"></div>
//    <div class="ph-info-row">
//        <div class="ph-icon-sq">${_svgCal}</div>
//        <span class="ph-info-date">${dateTxt}</span>
//    </div>
//    <div class="ph-source-tag">${_svgInfo} ${sourceLabel}</div>
//    ${notes ? `<span class="ph-info-notes">${notes}</span>` : ''}
//</div>`;
//}

//// ══════════════════════════════════════════════════════════════════
//// LIGHTBOX
//// ══════════════════════════════════════════════════════════════════
//let _lbPhotos = [];
//let _lbCaptions = [];
//let _lbIndex = 0;

//// شيل الـ query string وخذ الـ pathname فقط للمقارنة
//function _normUrl(url) {
//    try {
//        return new URL(url, location.origin).pathname;
//    } catch {
//        return url.split('?')[0];
//    }
//}

//function openLightbox(clickedSrc, group, caption) {

//    // ✅ جمع صور السكشن الحالي فقط حسب data-group
//    const cards = Array.from(
//        document.querySelectorAll(`.ph-photo-card[data-group="${group}"]`)
//    );

//    if (!cards.length) return;

//    // نستخدم getAttribute بدل .src عشان نتجنب الـ absolute URL اللي بيضيف domain
//    _lbPhotos = cards.map(c => {
//        const img = c.querySelector('img');
//        return img ? img.getAttribute('src') : '';
//    }).filter(Boolean);

//    _lbCaptions = cards.map(c => c.dataset.caption ?? '');

//    // مقارنة بدون query string وبدون domain
//    const clickedPath = _normUrl(clickedSrc);
//    _lbIndex = _lbPhotos.findIndex(s => _normUrl(s) === clickedPath);
//    if (_lbIndex === -1) _lbIndex = 0;

//    if (caption) _lbCaptions[_lbIndex] = caption;

//    _updateLB();

//    const lb = document.getElementById('ph-lightbox');
//    lb.classList.add('open');

//    document.addEventListener('keydown', _handleKey);
//}

//function closeLightbox() {
//    const lb = document.getElementById('ph-lightbox');
//    lb.classList.remove('open');

//    document.removeEventListener('keydown', _handleKey);
//    _lbPhotos = [];
//    _lbCaptions = [];
//}

//function closeLightboxBackground(e) {
//    if (e.target === document.getElementById('ph-lightbox')) closeLightbox();
//}

//function _updateLB() {
//    const img = document.getElementById('ph-lightbox-img');
//    const caption = document.getElementById('ph-lightbox-caption');
//    const counter = document.getElementById('ph-lightbox-counter');

//    // نشيل الـ ?t= عشان الصورة تظهر نظيفة في الـ lightbox
//    const src = (_lbPhotos[_lbIndex] ?? '').split('?')[0];
//    if (img) img.src = src;
//    if (caption) caption.textContent = _lbCaptions[_lbIndex] ?? '';
//    if (counter) counter.textContent =
//        _lbPhotos.length > 1 ? `${_lbIndex + 1} / ${_lbPhotos.length}` : '';
//}

//function prevPhoto(e) {
//    if (e) { e.stopPropagation(); e.preventDefault(); }
//    if (!_lbPhotos.length) return;
//    _lbIndex = (_lbIndex - 1 + _lbPhotos.length) % _lbPhotos.length;
//    _updateLB();
//}

//function nextPhoto(e) {
//    if (e) { e.stopPropagation(); e.preventDefault(); }
//    if (!_lbPhotos.length) return;
//    _lbIndex = (_lbIndex + 1) % _lbPhotos.length;
//    _updateLB();
//}

//function _handleKey(e) {
//    if (e.key === 'ArrowLeft') prevPhoto(null);
//    if (e.key === 'ArrowRight') nextPhoto(null);
//    if (e.key === 'Escape') closeLightbox();
//}

//// ══════════════════════════════════════════════════════════════════
//// مزامنة Extraoral مع تاب Photos
//// ══════════════════════════════════════════════════════════════════
//function syncExtraoralFlat(photoPath, label, photoId) {
//    const wrapper = document.getElementById('ph_extraoralWrapper');
//    if (!wrapper) return false;

//    document.getElementById('ph_extraoralEmpty')?.remove();

//    let gallery = document.getElementById('ph_extraoralGallery');
//    if (!gallery) {
//        gallery = document.createElement('div');
//        gallery.className = 'ph-flat-gallery';
//        gallery.id = 'ph_extraoralGallery';
//        wrapper.appendChild(gallery);
//    }

//    const today = new Date().toLocaleDateString('en-GB');
//    const caption = `${label} · ${today}`;

//    const card = document.createElement('div');
//    card.className = 'ph-photo-card';
//    card.id = `ph_eePhoto_${photoId}`;
//    card.dataset.group = 'extraoral';
//    card.dataset.caption = caption;
//    card.setAttribute('onclick', `openLightbox('${photoPath}', 'extraoral', '${caption}')`);

//    card.innerHTML = `
//        <div class="ph-img-wrap">
//            <img src="${photoPath}?t=${Date.now()}" alt="${label}" />
//            <div class="ph-type-pill">${label}</div>
//            <div class="ph-lock-icon">🔒</div>
//        </div>
//        ${_buildInfo(_svgPerson, label, today, 'Extraoral Exam')}`;

//    gallery.appendChild(card);
//    return true;
//}

//// ══════════════════════════════════════════════════════════════════
//// مزامنة Intraoral مع تاب Photos
//// ══════════════════════════════════════════════════════════════════
//function syncIntraoralFlat(photoPath, label, photoId) {
//    const wrapper = document.getElementById('ph_intraoralWrapper');
//    if (!wrapper) return false;

//    document.getElementById('ph_intraoralEmpty')?.remove();

//    let gallery = document.getElementById('ph_intraoralGallery');
//    if (!gallery) {
//        gallery = document.createElement('div');
//        gallery.className = 'ph-flat-gallery';
//        gallery.id = 'ph_intraoralGallery';
//        wrapper.appendChild(gallery);
//    }

//    const today = new Date().toLocaleDateString('en-GB');
//    const caption = `${label} · ${today}`;

//    const card = document.createElement('div');
//    card.className = 'ph-photo-card';
//    card.id = `ph_iePhoto_${photoId}`;
//    card.dataset.group = 'intraoral';
//    card.dataset.caption = caption;
//    card.setAttribute('onclick', `openLightbox('${photoPath}', 'intraoral', '${caption}')`);

//    card.innerHTML = `
//        <div class="ph-img-wrap">
//            <img src="${photoPath}?t=${Date.now()}" alt="${label}" />
//            <div class="ph-type-pill">${label}</div>
//            <div class="ph-lock-icon">🔒</div>
//        </div>
//        ${_buildInfo(_svgPerson, label, today, 'Intraoral Exam')}`;

//    gallery.appendChild(card);
//    return true;
//}

//// ══════════════════════════════════════════════════════════════════
//// مزامنة Radiograph مع تاب Photos
//// ══════════════════════════════════════════════════════════════════
//function syncRadToPhotosTab(photoId, photoPath, radType, fileName) {
//    const wrapper = document.getElementById('ph_radWrapper');
//    if (!wrapper) return;

//    document.getElementById('ph_radEmpty')?.remove();

//    let gallery = document.getElementById('ph_radGallery');
//    if (!gallery) {
//        gallery = document.createElement('div');
//        gallery.className = 'ph-flat-gallery';
//        gallery.id = 'ph_radGallery';
//        wrapper.appendChild(gallery);
//    }

//    const today = new Date().toLocaleDateString('en-GB');
//    const caption = `${radType} · ${today}`;

//    const card = document.createElement('div');
//    card.className = 'ph-photo-card';
//    card.id = `ph_rad_${photoId}`;
//    card.dataset.group = 'radiograph';
//    card.dataset.caption = caption;
//    card.setAttribute('onclick', `openLightbox('${photoPath}', 'radiograph', '${caption}')`);

//    card.innerHTML = `
//        <div class="ph-img-wrap">
//            <img src="${photoPath}" alt="${radType}" />
//            <div class="ph-type-pill">${radType}</div>
//            <div class="ph-lock-icon">🔒</div>
//        </div>
//        ${_buildInfo(_svgFile, fileName, today, 'Radiograph')}`;

//    gallery.appendChild(card);
//}

//// ══════════════════════════════════════════════════════════════════
//// حذف Radiograph من تاب Photos
//// ══════════════════════════════════════════════════════════════════
//function removeRadFromPhotosTab(photoId) {
//    document.getElementById(`ph_rad_${photoId}`)?.remove();

//    const gallery = document.getElementById('ph_radGallery');
//    if (!gallery) return;

//    if (gallery.querySelectorAll('.ph-photo-card').length === 0) {
//        gallery.remove();
//        const wrapper = document.getElementById('ph_radWrapper');
//        if (wrapper && !document.getElementById('ph_radEmpty')) {
//            const p = document.createElement('p');
//            p.className = 'ph-empty';
//            p.id = 'ph_radEmpty';
//            p.textContent = 'No radiographs uploaded yet.';
//            wrapper.appendChild(p);
//        }
//    }
//}

//// ══════════════════════════════════════════════════════════════════
//// حذف Extraoral/Intraoral من تاب Photos
//// ══════════════════════════════════════════════════════════════════
//function removePhotoFromPhotosTab(prefix, photoId) {
//    const map = {
//        galleryId: { ee: 'ph_extraoralGallery', ie: 'ph_intraoralGallery' },
//        emptyId: { ee: 'ph_extraoralEmpty', ie: 'ph_intraoralEmpty' },
//        wrapperId: { ee: 'ph_extraoralWrapper', ie: 'ph_intraoralWrapper' },
//        emptyText: { ee: 'No extraoral photos yet.', ie: 'No intraoral photos yet.' }
//    };

//    document.getElementById(`ph_${prefix}Photo_${photoId}`)?.remove();

//    const gallery = document.getElementById(map.galleryId[prefix]);
//    if (!gallery) return;

//    if (gallery.querySelectorAll('.ph-photo-card').length === 0) {
//        gallery.remove();
//        const wrapper = document.getElementById(map.wrapperId[prefix]);
//        if (wrapper && !document.getElementById(map.emptyId[prefix])) {
//            const p = document.createElement('p');
//            p.className = 'ph-empty';
//            p.id = map.emptyId[prefix];
//            p.textContent = map.emptyText[prefix];
//            wrapper.appendChild(p);
//        }
//    }
//}

//// ══════════════════════════════════════════════════════════════════
//// syncPhotoToPhotosTab — توافق مع IntraoralExam و ExtraoralExam
//// ══════════════════════════════════════════════════════════════════
//function syncPhotoToPhotosTab(prefix, sectionKey, photoPath, photoId) {
//    const labelText = sectionKey.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
//    if (prefix === 'ee') syncExtraoralFlat(photoPath, labelText, photoId);
//    if (prefix === 'ie') syncIntraoralFlat(photoPath, labelText, photoId);
//}

//// ══════════════════════════════════════════════════════════════════
//// رفع صورة Additional
//// ══════════════════════════════════════════════════════════════════
//function previewFileName(input, targetId) {
//    const el = document.getElementById(targetId);
//    if (el && input.files.length)
//        el.textContent = `Selected: ${input.files[0].name}`;
//}

//function uploadPatientPhoto() {
//    const fileInput = document.getElementById('ppFileInput');
//    const notes = document.getElementById('ppNotes')?.value ?? '';

//    if (!fileInput?.files?.length) {
//        showToast('Please select an image first.', 'error');
//        return;
//    }

//    const file = fileInput.files[0];
//    const fd = new FormData();
//    fd.append('patientId', PATIENT_ID);
//    fd.append('photo', file);
//    fd.append('notes', notes);
//    fd.append('__RequestVerificationToken', CSRF_TOKEN);

//    fetch('/PatientPhotos/Upload', { method: 'POST', body: fd })
//        .then(r => r.json())
//        .then(result => {
//            if (result.success) {
//                bootstrap.Modal.getInstance(
//                    document.getElementById('addPatientPhotoModal')
//                )?.hide();

//                document.getElementById('ppEmpty')?.remove();

//                let gallery = document.getElementById('patientPhotoGallery');
//                if (!gallery) {
//                    gallery = document.createElement('div');
//                    gallery.className = 'ph-flat-gallery';
//                    gallery.id = 'patientPhotoGallery';
//                    document.getElementById('ph_additionalWrapper')?.appendChild(gallery);
//                }

//                const today = new Date().toLocaleDateString('en-GB');
//                const caption = `${result.fileName} · ${today}`;

//                const card = document.createElement('div');
//                card.className = 'ph-photo-card';
//                card.id = `pp_${result.photoId}`;
//                card.dataset.group = 'additional';
//                card.dataset.caption = caption;
//                card.setAttribute('onclick',
//                    `openLightbox('${result.photoPath}', 'additional', '${caption}')`);

//                card.innerHTML = `
//                    <div class="ph-img-wrap">
//                        <img src="${result.photoPath}" alt="${result.fileName}" />
//                        <div class="ph-type-pill">Additional</div>
//                        <button class="ph-delete-btn"
//                                onclick="deletePatientPhoto(${result.photoId}, event)"
//                                title="Delete">&#x2715;</button>
//                    </div>
//                    ${_buildInfo(_svgFile, result.fileName, today, 'Additional', notes)}`;

//                gallery.appendChild(card);
//                showToast('Photo uploaded.', 'success');

//                fileInput.value = '';
//                if (document.getElementById('ppFileName'))
//                    document.getElementById('ppFileName').textContent = '';
//                if (document.getElementById('ppNotes'))
//                    document.getElementById('ppNotes').value = '';
//            } else {
//                showToast(result.message || 'Upload failed.', 'error');
//            }
//        })
//        .catch(() => showToast('Server error.', 'error'));
//}

//function deletePatientPhoto(photoId, e) {
//    if (e) { e.stopPropagation(); e.preventDefault(); }
//    if (!confirm('Delete this photo?')) return;

//    const fd = new FormData();
//    fd.append('photoId', photoId);
//    fd.append('patientId', PATIENT_ID);
//    fd.append('__RequestVerificationToken', CSRF_TOKEN);

//    fetch('/PatientPhotos/Delete', { method: 'POST', body: fd })
//        .then(r => r.json())
//        .then(result => {
//            if (result.success) {
//                document.getElementById(`pp_${photoId}`)?.remove();

//                const gallery = document.getElementById('patientPhotoGallery');
//                if (gallery && gallery.querySelectorAll('.ph-photo-card').length === 0) {
//                    gallery.remove();
//                    const wrapper = document.getElementById('ph_additionalWrapper');
//                    if (wrapper && !document.getElementById('ppEmpty')) {
//                        const p = document.createElement('p');
//                        p.className = 'ph-empty';
//                        p.id = 'ppEmpty';
//                        p.textContent = 'No additional photos uploaded yet.';
//                        wrapper.querySelector('.d-flex')?.insertAdjacentElement('afterend', p);
//                    }
//                }

//                showToast('Photo deleted.', 'success');
//            } else {
//                showToast('Error deleting.', 'error');
//            }
//        })
//        .catch(() => showToast('Server error.', 'error'));
//}

//// ══════════════════════════════════════════════════════════════════
//// Toast
//// ══════════════════════════════════════════════════════════════════
//if (typeof showToast === 'undefined') {
//    window.showToast = function (msg, type = 'success') {
//        const t = document.createElement('div');
//        t.textContent = msg;
//        Object.assign(t.style, {
//            position: 'fixed', bottom: '28px', right: '28px', zIndex: 999999,
//            background: type === 'success' ? '#1a237e' : '#c0392b',
//            color: '#fff', padding: '10px 22px', borderRadius: '8px',
//            fontSize: '13px', fontWeight: '600',
//            boxShadow: '0 4px 16px rgba(0,0,0,.18)', transition: 'opacity .4s'
//        });
//        document.body.appendChild(t);
//        setTimeout(() => {
//            t.style.opacity = '0';
//            setTimeout(() => t.remove(), 400);
//        }, 3000);
//    };
//}


// ════════════════════════════════════════════════════════════════
    //  CONFIG
    // ════════════════════════════════════════════════════════════════
    const PATIENT_ID = @patientId;   // Passed from Razor ViewBag
    const API_BASE = "/DentalChart/api";
    // ════════════════════════════════════════════════════════════════
    //  CONSTANTS
    // ════════════════════════════════════════════════════════════════
    const GROUP_COLORS = {
        disease:  getComputedStyle(document.documentElement).getPropertyValue('--disease').trim(),
    previous: getComputedStyle(document.documentElement).getPropertyValue('--previous').trim(),
    inside:   getComputedStyle(document.documentElement).getPropertyValue('--inside').trim(),
    others:   getComputedStyle(document.documentElement).getPropertyValue('--others').trim()
};

    const GROUP_LABELS = {
        disease:  "Disease",
    previous: "Previous Treatment",
    inside:   "In-Hospital",
    others:   "Others"
};

    const TOOLS = [
    {id:"caries",                    label:"Caries",                              group:"disease",  glyph:"caries",          scope:"surface" },
    {id:"secondary_caries",          label:"Secondary Caries",                    group:"disease",  glyph:"secondary",        scope:"surface" },
    {id:"periapical_lesion",         label:"Periapical Lesion",                   group:"disease",  glyph:"periapical",       scope:"root"    },
    {id:"crack_tooth",               label:"Crack Tooth",                         group:"disease",  glyph:"crack",            scope:"surface" },
    {id:"tooth_mobility",            label:"Tooth Mobility",                      group:"disease",  glyph:"mobility",         scope:"whole"   },
    {id:"remaining_root",            label:"Remaining Root",                      group:"disease",  glyph:"remainingRoot",    scope:"whole"   },
    {id:"surface_loss",              label:"Tooth Surface Loss",                  group:"disease",  glyph:"surfaceLoss",      scope:"surface" },

    {id:"amalgam_prev",              label:"Amalgam",                             group:"previous", glyph:"restoration",      scope:"surface" },
    {id:"composite_prev",            label:"Composite Restoration",               group:"previous", glyph:"restoration",      scope:"surface" },
    {id:"gic_prev",                  label:"GIC",                                 group:"previous", glyph:"restoration",      scope:"surface" },
    {id:"temporary_restoration_prev",label:"Temporary Restoration",               group:"previous", glyph:"restoration",      scope:"surface" },
    {id:"metal_crown_prev",          label:"Metal Crown",                         group:"previous", glyph:"crown",            scope:"whole"   },
    {id:"zirconia_crown_prev",       label:"Zirconia Crown",                      group:"previous", glyph:"crown",            scope:"whole"   },
    {id:"emax_crown_prev",           label:"Emax Crown",                          group:"previous", glyph:"crown",            scope:"whole"   },
    {id:"pfm_crown_prev",            label:"PFM Crown",                           group:"previous", glyph:"crown",            scope:"whole"   },
    {id:"resin_bonded_bridge_prev",  label:"Resin Bonded Bridge",                 group:"previous", glyph:"bridge",           scope:"whole"   },
    {id:"temporary_crown_prev",      label:"Temporary Crown",                     group:"previous", glyph:"crown",            scope:"whole"   },
    {id:"root_canal_prev",           label:"Root Canal Treatment",                group:"previous", glyph:"rct",              scope:"root"    },
    {id:"vital_pulp_therapy_prev",   label:"Vital Pulp Therapy",                  group:"previous", glyph:"vpt",              scope:"root"    },
    {id:"post_prev",                 label:"Post",                                group:"previous", glyph:"post",             scope:"root"    },
    {id:"partial_restoration_prev",  label:"Partial Restoration",                 group:"previous", glyph:"restoration",      scope:"surface" },
    {id:"stabilization_splint_prev", label:"Stabilization Splint",                group:"previous", glyph:"splint",           scope:"whole"   },
    {id:"partial_acrylic_denture_prev",label:"Partial Acrylic Denture",           group:"previous", glyph:"denture",          scope:"whole"   },
    {id:"cc_denture_prev",           label:"Cobalt Chromium Partial Denture",     group:"previous", glyph:"denture",          scope:"whole"   },
    {id:"complete_denture_prev",     label:"Complete Denture",                    group:"previous", glyph:"denture",          scope:"whole"   },
    {id:"stainless_crown_prev",      label:"Stainless Steel Crown",               group:"previous", glyph:"crown",            scope:"whole"   },
    {id:"space_maintainer_prev",     label:"Space Maintainer",                    group:"previous", glyph:"spaceMaintainer",  scope:"whole"   },
    {id:"pulpotomy_prev",            label:"Pulpotomy",                           group:"previous", glyph:"rct",              scope:"root"    },
    {id:"pulpectomy_prev",           label:"Pulpectomy",                          group:"previous", glyph:"rct",              scope:"root"    },
    {id:"extraction_prev",           label:"Extraction",                          group:"previous", glyph:"extracted",        scope:"whole"   },

    {id:"amalgam_in",                label:"Amalgam",                             group:"inside",   glyph:"restoration",      scope:"surface" },
    {id:"composite_in",              label:"Composite Restoration",               group:"inside",   glyph:"restoration",      scope:"surface" },
    {id:"gic_in",                    label:"GIC",                                 group:"inside",   glyph:"restoration",      scope:"surface" },
    {id:"temporary_restoration_in",  label:"Temporary Restoration",               group:"inside",   glyph:"restoration",      scope:"surface" },
    {id:"metal_crown_in",            label:"Metal Crown",                         group:"inside",   glyph:"crown",            scope:"whole"   },
    {id:"ceramic_crown_in",          label:"Ceramic Crown",                       group:"inside",   glyph:"crown",            scope:"whole"   },
    {id:"emax_crown_in",             label:"Emax Crown",                          group:"inside",   glyph:"crown",            scope:"whole"   },
    {id:"pfm_crown_in",              label:"PFM Crown",                           group:"inside",   glyph:"crown",            scope:"whole"   },
    {id:"resin_bonded_bridge_in",    label:"Resin Bonded Bridge",                 group:"inside",   glyph:"bridge",           scope:"whole"   },
    {id:"temporary_crown_in",        label:"Temporary Crown",                     group:"inside",   glyph:"crown",            scope:"whole"   },
    {id:"root_canal_in",             label:"Root Canal Treatment",                group:"inside",   glyph:"rct",              scope:"root"    },
    {id:"vpt_in",                    label:"Vital Pulp Therapy",                  group:"inside",   glyph:"vpt",              scope:"root"    },
    {id:"post_in",                   label:"Post",                                group:"inside",   glyph:"post",             scope:"root"    },
    {id:"partial_restoration_in",    label:"Partial Restoration",                 group:"inside",   glyph:"restoration",      scope:"surface" },
    {id:"stabilization_splint_in",   label:"Stabilization Splint",                group:"inside",   glyph:"splint",           scope:"whole"   },
    {id:"partial_denture_in",        label:"Partial Acrylic Denture",             group:"inside",   glyph:"denture",          scope:"whole"   },
    {id:"cc_denture_in",             label:"Cobalt Chromium Partial Denture",     group:"inside",   glyph:"denture",          scope:"whole"   },
    {id:"complete_denture_in",       label:"Complete Denture",                    group:"inside",   glyph:"denture",          scope:"whole"   },
    {id:"stainless_crown_in",        label:"Stainless Steel Crown",               group:"inside",   glyph:"crown",            scope:"whole"   },
    {id:"space_maintainer_in",       label:"Space Maintainer",                    group:"inside",   glyph:"spaceMaintainer",  scope:"whole"   },
    {id:"pulpotomy_in",              label:"Pulpotomy",                           group:"inside",   glyph:"rct",              scope:"root"    },
    {id:"pulpectomy_in",             label:"Pulpectomy",                          group:"inside",   glyph:"rct",              scope:"root"    },
    {id:"extraction_in",             label:"Extraction",                          group:"inside",   glyph:"extracted",        scope:"whole"   },

    {id:"impacted_tooth",            label:"Impacted Tooth",                      group:"others",   glyph:"impacted",         scope:"whole"   },
    {id:"soft_tissue_impacted",      label:"Soft Tissue Impacted Tooth",          group:"others",   glyph:"softImpacted",     scope:"whole"   },
    {id:"partially_erupted",         label:"Partially Erupted Tooth",             group:"others",   glyph:"partiallyErupted", scope:"whole"   },
    {id:"teeth_transposition",       label:"Teeth Transposition",                 group:"others",   glyph:"transposition",    scope:"whole"   },
    {id:"teeth_change",              label:"Teeth Change Permanent/Deciduous",    group:"others",   glyph:"teethChange",      scope:"whole"   },
    {id:"extracted_tooth",           label:"Extracted Tooth",                     group:"others",   glyph:"extracted",        scope:"whole"   },
    {id:"others_text",               label:"Others / Text",                       group:"others",   glyph:"note",             scope:"surface" }
    ];

    const POS_LABELS  = [8,7,6,5,4,3,2,1,1,2,3,4,5,6,7,8];
    const TOOTH_TYPES = [
    "molar3","molar2","molar1","premolar2","premolar1","canine","lateral","central",
    "central","lateral","canine","premolar1","premolar2","molar1","molar2","molar3"
    ];
    const UPPER_IDS = Array.from({length:16}, (_,i)=>`U${i}`);
    const LOWER_IDS = Array.from({length:16}, (_,i)=>`L${i}`);

    // ════════════════════════════════════════════════════════════════
    //  STATE
    // ════════════════════════════════════════════════════════════════
    const chartData = { };
    let selectedToolId = null;
    let selectedTarget = null;

    function makeSlot() { return {disease:null, previous:null, inside:null, others:null, note:"" }; }

    function makeTooth(id) {
    return {
        id,
        whole: makeSlot(),
    areas: {
        buccal: makeSlot(), lingual: makeSlot(), palatal: makeSlot(),
    mesial: makeSlot(), distal: makeSlot(), occlusal: makeSlot(), root: makeSlot()
        }
    };
}

[...UPPER_IDS, ...LOWER_IDS].forEach(id => {chartData[id] = makeTooth(id); });

    // ════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════
    function getTool(id)      { return TOOLS.find(t => t.id === id) || null; }
    function getToothType(id) { return TOOTH_TYPES[parseInt(id.slice(1), 10)]; }
    function isSelected(tid, area) {
    return selectedTarget && selectedTarget.toothId === tid && selectedTarget.area === area;
}

    function hexToRgba(hex, alpha) {
    const h = hex.replace('#','');
    const b = parseInt(h, 16);
    return `rgba(${(b >> 16) & 255},${(b >> 8) & 255},${b & 255},${alpha})`;
}

    function getMarks(slot) {
    return ["disease","previous","inside","others"]
        .filter(g => slot[g])
        .map(g => ({group:g, tool:getTool(slot[g]) }))
        .filter(x => x.tool);
}

    function getWholeMarks(tid)        { return getMarks(chartData[tid].whole); }
    function getAreaMarks(tid, area)   { return getMarks(chartData[tid].areas[area]); }

    function surfaceTint(tid, area) {
    const m = getAreaMarks(tid, area);
    if (!m.length)    return "#ffffff";
    if (m.length===1) return hexToRgba(GROUP_COLORS[m[0].group], .24);
    return "#eef2f7";
}

    function escapeHtml(s) {
    return String(s)
    .replaceAll("&","&amp;").replaceAll("<","&lt;")
        .replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;");
}

    // ════════════════════════════════════════════════════════════════
    //  ICONS
    // ════════════════════════════════════════════════════════════════
    function iconSvg(glyph, color, size=24) {
    const c  = `stroke="${color}" fill="none" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"`;
    const f  = `fill="${color}" stroke="${color}" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"`;
    const map = {
        caries:          `<circle cx="16" cy="16" r="6" ${f} />`,
    secondary:       `<circle cx="16" cy="16" r="7.5" ${c} /><circle cx="16" cy="16" r="3.5" ${f} />`,
    periapical:      `<ellipse cx="16" cy="20" rx="7" ry="4.5" ${f} />`,
    crack:           `<path d="M18 6 L13 15 L18 15 L14 26 L21 16 L17 16 Z" ${f} />`,
    mobility:        `<path d="M6 16 H26" ${c} /><path d="M9 12 L5 16 L9 20" ${c} /><path d="M23 12 L27 16 L23 20" ${c} />`,
    remainingRoot:   `<path d="M11 8 H21 L19 25 H13 Z" ${f} />`,
    surfaceLoss:     `<rect x="8" y="10" width="16" height="12" rx="4" ${c} /><path d="M10 22 L22 10" ${c} />`,
    restoration:     `<path d="M9 18 Q16 8 23 18 Q16 24 9 18 Z" ${f} />`,
    crown:           `<path d="M8 19 L10 11 L14 15 L16 9 L18 15 L22 11 L24 19 Q22 23 16 23 Q10 23 8 19 Z" ${f} />`,
    bridge:          `<circle cx="9" cy="18" r="4" ${c} /><circle cx="23" cy="18" r="4" ${c} /><rect x="9" y="16" width="14" height="4" rx="2" ${f} />`,
    rct:             `<path d="M11 8 V25" ${c} /><path d="M16 8 V25" ${c} /><path d="M21 8 V25" ${c} />`,
    vpt:             `<circle cx="16" cy="16" r="6" ${c} /><path d="M16 11 V21 M11 16 H21" ${c} />`,
    post:            `<rect x="14" y="7" width="4" height="18" rx="1.5" ${f} />`,
    splint:          `<path d="M7 18 Q16 11 25 18" ${c} /><path d="M7 21 Q16 14 25 21" ${c} />`,
    denture:         `<path d="M7 22 Q16 9 25 22" ${c} /><circle cx="10.5" cy="19.5" r="1.4" ${f} /><circle cx="16" cy="16.5" r="1.4" ${f} /><circle cx="21.5" cy="19.5" r="1.4" ${f} />`,
    spaceMaintainer: `<path d="M6 18 H26" ${c} /><circle cx="8" cy="18" r="2.2" ${f} /><circle cx="24" cy="18" r="2.2" ${f} />`,
    impacted:        `<path d="M16 7 Q23 10 23 18 Q23 25 16 26 Q9 25 9 18 Q9 10 16 7 Z" stroke="${color}" fill="none" stroke-width="2.2" stroke-dasharray="3 2" />`,
    softImpacted:    `<path d="M16 7 Q23 10 23 18 Q23 25 16 26 Q9 25 9 18 Q9 10 16 7 Z" stroke="${color}" fill="none" stroke-width="2.2" stroke-dasharray="3 2" /><path d="M8 20 Q16 14 24 20" ${c} />`,
    partiallyErupted:`<path d="M11 12 Q16 6 21 12 L20 22 L12 22 Z" ${c} /><path d="M7 18 Q16 15 25 18" ${c} />`,
    transposition:   `<path d="M8 10 L24 24" ${c} /><path d="M24 10 L8 24" ${c} />`,
    teethChange:     `<path d="M11 10 A8 8 0 0 1 22 12" ${c} /><path d="M22 22 A8 8 0 0 1 11 20" ${c} /><path d="M21 9 L23 13 L19 13" ${c} /><path d="M12 23 L10 19 L14 19" ${c} />`,
    extracted:       `<path d="M9 9 L23 23" ${c} /><path d="M23 9 L9 23" ${c} />`,
    note:            `<rect x="10" y="7" width="12" height="18" rx="2" ${c} /><path d="M13 12 H19 M13 16 H19 M13 20 H17" ${c} />`
    };
    return `<svg width="${size}" height="${size}" viewBox="0 0 32 32" xmlns="http://www.w3.org/2000/svg">${map[glyph] || map.note}</svg>`;
}

    // ════════════════════════════════════════════════════════════════
    //  TOOLBAR
    // ════════════════════════════════════════════════════════════════
    function renderToolbar() {
    const grouped = {
        disease:  TOOLS.filter(t=>t.group==="disease"),
        previous: TOOLS.filter(t=>t.group==="previous"),
        inside:   TOOLS.filter(t=>t.group==="inside"),
        others:   TOOLS.filter(t=>t.group==="others")
    };
    document.getElementById("toolbar").innerHTML = `
    <div class="toolbar-row">
        ${Object.keys(grouped).map(group => `
            <div class="group-block">
              <div class="group-title">
                <span class="group-dot" style="background:${GROUP_COLORS[group]}"></span>
                ${GROUP_LABELS[group]}
              </div>
              <div class="tool-strip">
                ${grouped[group].map(tool => `
                  <button class="tool-btn ${selectedToolId === tool.id ? 'active' : ''}"
                          onclick="selectTool('${tool.id}')" title="${escapeHtml(tool.label)}">
                    ${iconSvg(tool.glyph, GROUP_COLORS[group], 24)}
                    <span class="tip">${escapeHtml(tool.label)}</span>
                  </button>
                `).join("")}
              </div>
            </div>
          `).join("")}
    </div>`;
}

    function selectTool(toolId) {selectedToolId = toolId; renderAll(); }

    // ════════════════════════════════════════════════════════════════
    //  SVG SHAPES
    // ════════════════════════════════════════════════════════════════
    function frontShape(type, arch) {
    const upper = arch === "upper";
    const map = {
        molar3:   upper ? {crown:`M15 20 Q16 8 24 8 Q28 6 32 10 Q36 6 40 8 Q48 8 49 20 L47 34 Q32 40 17 34 Z`, root:`M20 34 Q22 48 18 70 Q24 66 28 48 L30 34 M44 34 Q42 48 46 70 Q40 66 36 48 L34 34` }
    : {crown:`M17 20 Q18 9 25 9 Q29 7 32 10 Q35 7 39 9 Q46 9 47 20 L45 34 Q32 39 19 34 Z`, root:`M24 34 Q24 52 22 74 L28 74 Q31 54 31 34 M40 34 Q40 52 42 74 L36 74 Q33 54 33 34` },
    molar2:   upper ? {crown:`M17 20 Q18 9 25 9 Q29 7 32 10 Q35 7 39 9 Q46 9 47 20 L45 34 Q32 39 19 34 Z`, root:`M24 34 Q24 53 21 74 Q28 69 30 48 L31 34 M40 34 Q40 53 43 74 Q36 69 34 48 L33 34` }
    : {crown:`M18 20 Q18 10 25 10 Q29 8 32 11 Q35 8 39 10 Q46 10 46 20 L45 33 Q32 37 19 33 Z`, root:`M26 33 Q26 55 24 76 L29 76 Q31 56 31 33 M38 33 Q38 55 40 76 L35 76 Q33 56 33 33` },
    molar1:   upper ? {crown:`M18 20 Q18 10 25 10 Q29 8 32 11 Q35 8 39 10 Q46 10 46 20 L45 33 Q32 38 19 33 Z`, root:`M26 33 Q26 55 24 76 L29 76 Q31 56 31 33 M38 33 Q38 55 40 76 L35 76 Q33 56 33 33` }
    : {crown:`M19 20 Q20 11 26 11 Q30 9 32 12 Q34 9 38 11 Q44 11 45 20 L44 33 Q32 37 20 33 Z`, root:`M27 33 Q27 56 26 78 L30 78 Q31 59 31 33 M37 33 Q37 56 38 78 L34 78 Q33 59 33 33` },
    premolar2:upper ? {crown:`M23 18 Q24 9 32 8 Q40 9 41 18 L40 31 Q32 35 24 31 Z`, root:`M29 31 Q27 49 26 76 L31 78 L35 76 Q34 49 35 31 Z` }
    : {crown:`M24 18 Q24 10 32 9 Q40 10 40 18 L39 31 Q32 34 25 31 Z`, root:`M29 31 Q28 49 29 78 L35 77 Q35 49 35 31 Z` },
    premolar1:upper ? {crown:`M24 18 Q25 9 32 8 Q39 9 40 18 L39 31 Q32 35 25 31 Z`, root:`M29 31 Q28 49 29 79 L35 78 Q35 49 35 31 Z` }
    : {crown:`M25 18 Q25 10 32 9 Q39 10 39 18 L38 31 Q32 34 26 31 Z`, root:`M30 31 Q29 51 30 80 L34 80 Q35 51 34 31 Z` },
    canine:   upper ? {crown:`M28 16 L32 7 L36 16 L38 31 Q32 35 26 31 Z`, root:`M30 31 Q29 50 30 82 L34 82 Q35 50 34 31 Z` }
    : {crown:`M29 16 L32 8 L35 16 L37 30 Q32 34 27 30 Z`, root:`M30 30 Q29 52 31 82 L34 82 Q35 52 34 30 Z` },
    lateral:  upper ? {crown:`M27 16 Q28 9 32 9 Q36 9 37 16 L36 30 Q32 33 28 30 Z`, root:`M30 30 Q28 48 29 80 L34 80 Q35 48 34 30 Z` }
    : {crown:`M28 17 Q28 11 32 10 Q36 11 36 17 L35 29 Q32 32 29 29 Z`, root:`M30 29 Q29 48 30 79 L34 79 Q35 48 34 29 Z` },
    central:  upper ? {crown:`M26 16 Q27 8 32 8 Q37 8 38 16 L37 30 Q32 34 27 30 Z`, root:`M29 30 Q28 48 29 80 L35 80 Q36 48 35 30 Z` }
    : {crown:`M28 17 Q28 10 32 10 Q36 10 36 17 L35 29 Q32 32 29 29 Z`, root:`M30 29 Q29 49 31 80 L34 80 Q35 49 34 29 Z` }
    };
    return map[type];
}

    function occlusalOutline(type) {
    return {
        molar3:   "M10 16 Q10 7 20 6 L44 6 Q54 7 54 16 L54 32 Q53 41 44 42 L20 42 Q10 41 10 32 Z",
    molar2:   "M11 16 Q11 8 20 7 L44 7 Q53 8 53 16 L53 32 Q52 40 44 41 L20 41 Q11 40 11 32 Z",
    molar1:   "M12 16 Q12 8 21 8 L43 8 Q52 8 52 16 L52 32 Q52 40 43 40 L21 40 Q12 40 12 32 Z",
    premolar2:"M17 11 Q22 6 32 6 Q42 6 47 11 Q50 16 47 31 Q42 38 32 38 Q22 38 17 31 Q14 16 17 11 Z",
    premolar1:"M18 10 Q23 6 32 6 Q41 6 46 10 Q49 16 46 32 Q41 38 32 38 Q23 38 18 32 Q15 16 18 10 Z",
    canine:   "M21 10 Q25 6 32 6 Q39 6 43 10 Q46 16 44 31 Q39 38 32 39 Q25 38 20 31 Q18 16 21 10 Z",
    lateral:  "M23 12 Q27 8 32 8 Q37 8 41 12 Q43 17 41 30 Q37 35 32 35 Q27 35 23 30 Q21 17 23 12 Z",
    central:  "M22 11 Q26 7 32 7 Q38 7 42 11 Q45 17 42 31 Q38 36 32 36 Q26 36 22 31 Q19 17 22 11 Z"
    }[type];
}

    // ════════════════════════════════════════════════════════════════
    //  OVERLAYS & BADGES
    // ════════════════════════════════════════════════════════════════
    function wholeToothOverlayFront(tid) {
    const wm = getWholeMarks(tid);
    if (!wm.length && !chartData[tid].whole.note) return "";
    let p = "";
    const extracted   = wm.find(m=>m.tool.glyph==="extracted");
    const impacted    = wm.find(m=>m.tool.glyph==="impacted"||m.tool.glyph==="softImpacted");
    const crown       = wm.find(m=>m.tool.glyph==="crown");
    const mobility    = wm.find(m=>m.tool.glyph==="mobility");
    const remainRoot  = wm.find(m=>m.tool.glyph==="remainingRoot");
    const partErupt   = wm.find(m=>m.tool.glyph==="partiallyErupted");
    if (crown)     { const c=GROUP_COLORS[crown.group];    p+=`<rect x="17" y="9" width="30" height="24" rx="7" fill="${hexToRgba(c,.16)}" stroke="${c}" stroke-width="2" />`; }
    if (partErupt) { const c=GROUP_COLORS[partErupt.group]; p+=`<path d="M14 25 Q32 18 50 25" fill="none" stroke="${c}" stroke-width="3" />`; }
    if (impacted)  { const c=GROUP_COLORS[impacted.group];  p+=`<rect x="10" y="6" width="44" height="72" rx="14" fill="none" stroke="${c}" stroke-dasharray="4 3" stroke-width="2.5" />`; }
    if (remainRoot){ const c=GROUP_COLORS[remainRoot.group];p+=`<rect x="25" y="37" width="14" height="40" rx="4" fill="${hexToRgba(c,.18)}" stroke="${c}" stroke-width="2.2" />`; }
    if (mobility)  { const c=GROUP_COLORS[mobility.group];  p+=`<path d="M8 18 H56" stroke="${c}" stroke-width="3" stroke-linecap="round" /><path d="M12 14 L8 18 L12 22" stroke="${c}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" /><path d="M52 14 L56 18 L52 22" stroke="${c}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />`; }
    if (extracted) { const c=GROUP_COLORS[extracted.group]; p+=`<path d="M14 12 L50 74" stroke="${c}" stroke-width="4.5" stroke-linecap="round" /><path d="M50 12 L14 74" stroke="${c}" stroke-width="4.5" stroke-linecap="round" />`; }
    const positions = [{x:6,y:4},{x:42,y:4},{x:6,y:60},{x:42,y:60}];
    wm.slice(0,4).forEach((m,i) => {
        const pp = positions[i];
    p += `<g transform="translate(${pp.x},${pp.y})"><rect x="0" y="0" width="16" height="16" rx="4" fill="#ffffff" stroke="${GROUP_COLORS[m.group]}" stroke-width="1.5" /><g transform="translate(0,0)">${iconSvg(m.tool.glyph, GROUP_COLORS[m.group], 16)}</g></g>`;
    });
    if (chartData[tid].whole.note) {p += `<g transform="translate(24,58)"><rect x="0" y="0" width="16" height="16" rx="4" fill="#ffffff" stroke="#6b7280" stroke-width="1.5"/><g>${iconSvg("note", "#6b7280", 16)}</g></g>`; }
    return p;
}

    function wholeToothOverlayOcclusal(tid) {
    const wm = getWholeMarks(tid);
    if (!wm.length && !chartData[tid].whole.note) return "";
    let p = "";
    const extracted = wm.find(m=>m.tool.glyph==="extracted");
    const impacted  = wm.find(m=>m.tool.glyph==="impacted"||m.tool.glyph==="softImpacted");
    const mobility  = wm.find(m=>m.tool.glyph==="mobility");
    const bridge    = wm.find(m=>m.tool.glyph==="bridge");
    const denture   = wm.find(m=>m.tool.glyph==="denture");
    if (impacted)  { const c=GROUP_COLORS[impacted.group];  p+=`<rect x="6" y="4" width="52" height="40" rx="14" fill="none" stroke="${c}" stroke-dasharray="4 3" stroke-width="2.4" />`; }
    if (mobility)  { const c=GROUP_COLORS[mobility.group];  p+=`<path d="M10 24 H54" stroke="${c}" stroke-width="2.8" stroke-linecap="round" /><path d="M15 20 L10 24 L15 28" stroke="${c}" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round" /><path d="M49 20 L54 24 L49 28" stroke="${c}" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round" />`; }
    if (bridge)    { const c=GROUP_COLORS[bridge.group];    p+=`<path d="M12 24 H52" stroke="${c}" stroke-width="2.8" />`; }
    if (denture)   { const c=GROUP_COLORS[denture.group];   p+=`<path d="M12 34 Q32 14 52 34" fill="none" stroke="${c}" stroke-width="2.8" />`; }
    if (extracted) { const c=GROUP_COLORS[extracted.group]; p+=`<path d="M12 10 L52 38" stroke="${c}" stroke-width="4.5" stroke-linecap="round" /><path d="M52 10 L12 38" stroke="${c}" stroke-width="4.5" stroke-linecap="round" />`; }
    const positions = [{x:8,y:2},{x:24,y:2},{x:40,y:2},{x:24,y:30}];
    wm.slice(0,4).forEach((m,i) => {
        const pp = positions[i];
    p += `<g transform="translate(${pp.x},${pp.y})"><rect x="0" y="0" width="14" height="14" rx="4" fill="#ffffff" stroke="${GROUP_COLORS[m.group]}" stroke-width="1.3" /><g transform="translate(-1,-1)">${iconSvg(m.tool.glyph, GROUP_COLORS[m.group], 16)}</g></g>`;
    });
    if (chartData[tid].whole.note) {p += `<g transform="translate(44,30)"><rect x="0" y="0" width="14" height="14" rx="4" fill="#ffffff" stroke="#6b7280" stroke-width="1.3"/><g transform="translate(-1,-1)">${iconSvg("note", "#6b7280", 16)}</g></g>`; }
    return p;
}

    function areaBadges(tid, area, points) {
    const marks = getAreaMarks(tid, area);
    let out = "";
    marks.slice(0,4).forEach((m,i) => {
        const pt = points[i];
    out += `<g transform="translate(${pt.x-8},${pt.y-8})"><circle cx="8" cy="8" r="8" fill="#ffffff" stroke="${GROUP_COLORS[m.group]}" stroke-width="1.6" /><g>${iconSvg(m.tool.glyph, GROUP_COLORS[m.group], 16)}</g></g>`;
    });
    if (chartData[tid].areas[area].note) {
        const pt = points[Math.min(marks.length, points.length-1)];
    out += `<g transform="translate(${pt.x-8},${pt.y-8})"><circle cx="8" cy="8" r="8" fill="#ffffff" stroke="#6b7280" stroke-width="1.6" /><g>${iconSvg("note", "#6b7280", 16)}</g></g>`;
    }
    return out;
}

    function badgeSpotsForOcclusalPart(key) {
    return {
        mesial:  [{x:15,y:14},{x:15,y:24},{x:21,y:19},{x:19,y:30}],
    distal:  [{x:49,y:14},{x:49,y:24},{x:43,y:19},{x:45,y:30}],
    occlusal:[{x:28,y:17},{x:36,y:17},{x:28,y:26},{x:36,y:26}],
    buccal:  [{x:23,y:31},{x:31,y:31},{x:39,y:31},{x:31,y:36}],
    palatal: [{x:23,y:10},{x:31,y:10},{x:39,y:10},{x:31,y:15}],
    lingual: [{x:23,y:10},{x:31,y:10},{x:39,y:10},{x:31,y:15}]
    }[key];
}

    // ════════════════════════════════════════════════════════════════
    //  SVG TOOTH RENDERERS
    // ════════════════════════════════════════════════════════════════
    function frontToothSvg(tid, arch) {
    const type = getToothType(tid);
    const s    = frontShape(type, arch);
    const cf   = getAreaMarks(tid,"buccal").length ? surfaceTint(tid,"buccal") : "#f5f6f8";
    const rf   = getAreaMarks(tid,"root").length   ? surfaceTint(tid,"root")   : "#f5f6f8";
    const cs   = isSelected(tid,"buccal") ? "#111827" : "#c4ccd7";
    const rs   = isSelected(tid,"root")   ? "#111827" : "#c4ccd7";
    return `
    <svg class="tooth-svg" viewBox="0 0 64 88" xmlns="http://www.w3.org/2000/svg">
        <path d="${s.crown}" fill="${cf}" stroke="${cs}" stroke-width="${isSelected(tid,'buccal')?2.4:1.6}" onclick="selectArea('${tid}','buccal')" />
        <path d="${s.root}" fill="${rf}" stroke="${rs}" stroke-width="${isSelected(tid,'root')?2.4:1.6}" onclick="selectArea('${tid}','root')" />
        ${wholeToothOverlayFront(tid)}
        ${areaBadges(tid, "buccal", [{ x: 22, y: 16 }, { x: 40, y: 16 }, { x: 22, y: 28 }, { x: 40, y: 28 }])}
        ${areaBadges(tid, "root", [{ x: 22, y: 44 }, { x: 40, y: 44 }, { x: 22, y: 60 }, { x: 40, y: 60 }])}
    </svg>`;
}

    function occlusalToothSvg(tid, arch) {
    const type       = getToothType(tid);
    const outline    = occlusalOutline(type);
    const insideName = arch === "upper" ? "palatal" : "lingual";
    const parts = [
    {key:"mesial",    path:"M14 14 L25 14 L23 33 L13 29 Z" },
    {key:"distal",    path:"M39 14 L50 14 L51 29 L41 33 Z" },
    {key:"occlusal",  path:"M25 16 L39 16 L38 30 L26 30 Z" },
    {key:"buccal",    path:"M18 29 L46 29 L42 36 L22 36 Z" },
    {key:insideName,  path:"M19 12 L45 12 L39 18 L25 18 Z" }
    ];
    return `
    <svg class="tooth-svg" viewBox="0 0 64 48" xmlns="http://www.w3.org/2000/svg">
        <path d="${outline}" fill="#fafafa" stroke="#d0d6df" stroke-width="1.4" />
        ${parts.map(pp => {
            const fill = getAreaMarks(tid, pp.key).length ? surfaceTint(tid, pp.key) : "#ffffff";
            const stroke = isSelected(tid, pp.key) ? "#111827" : "#c4ccd7";
            return `<path d="${pp.path}" fill="${fill}" stroke="${stroke}" stroke-width="${isSelected(tid, pp.key) ? 2.2 : 1.2}" onclick="selectArea('${tid}','${pp.key}')"/>`;
        }).join("")}
        ${wholeToothOverlayOcclusal(tid)}
        ${parts.map(pp => areaBadges(tid, pp.key, badgeSpotsForOcclusalPart(pp.key))).join("")}
    </svg>`;
}

    // ════════════════════════════════════════════════════════════════
    //  INTERACTIONS
    // ════════════════════════════════════════════════════════════════
    function selectArea(tid, area) {
        selectedTarget = { toothId: tid, area };
    if (selectedToolId) applyToolToSelectedTarget();
    renderAll();
}

    function applyToolToSelectedTarget() {
    if (!selectedToolId || !selectedTarget) return;
    const tool  = getTool(selectedToolId);
    const tooth = chartData[selectedTarget.toothId];
    if (!tool) return;
    if (tool.scope === "whole")       {tooth.whole[tool.group] = tool.id; }
    else if (tool.scope === "root")   {tooth.areas.root[tool.group] = tool.id; selectedTarget.area = "root"; }
    else                              {tooth.areas[selectedTarget.area][tool.group] = tool.id; }
}

    function clearSelectedTarget() {
    if (!selectedTarget) return;
    chartData[selectedTarget.toothId].areas[selectedTarget.area] = makeSlot();
    renderAll();
}

    function clearSelectedTooth() {
    if (!selectedTarget) return;
    const id = selectedTarget.toothId;
    chartData[id] = makeTooth(id);
    renderAll();
}

    function clearAllChart() {
        [...UPPER_IDS, ...LOWER_IDS].forEach(id => { chartData[id] = makeTooth(id); });
    renderAll();
}

    function saveNote() {
    if (!selectedTarget) return;
    const text = document.getElementById("noteBox").value.trim();
    chartData[selectedTarget.toothId].areas[selectedTarget.area].note = text;
    renderAll();
}

    function removeGroupFromArea(group) {
    if (!selectedTarget) return;
    chartData[selectedTarget.toothId].areas[selectedTarget.area][group] = null;
    renderAll();
}

    function removeWholeGroup(tid, group) {
        chartData[tid].whole[group] = null;
    renderAll();
}

    function clearAreaNote() {
    if (!selectedTarget) return;
    chartData[selectedTarget.toothId].areas[selectedTarget.area].note = "";
    renderAll();
}

    // ════════════════════════════════════════════════════════════════
    //  RENDER
    // ════════════════════════════════════════════════════════════════
    function renderSideInfo() {
        document.getElementById("selectedToolLabel").textContent = selectedToolId ? getTool(selectedToolId).label : "—";
    document.getElementById("selectedToothLabel").textContent = selectedTarget ? selectedTarget.toothId : "—";
    document.getElementById("selectedAreaLabel").textContent  = selectedTarget ? selectedTarget.area    : "—";

    const detailList = document.getElementById("detailList");
    if (!selectedTarget) {
        detailList.innerHTML = `<div style="color:#6b7280;font-size:13px;">Nothing selected.</div>`;
    document.getElementById("noteBox").value = "";
    document.getElementById("jsonOut").textContent = JSON.stringify(chartData, null, 2);
    return;
    }

    const {toothId, area} = selectedTarget;
    const areaSlot  = chartData[toothId].areas[area];
    const wholeSlot = chartData[toothId].whole;
    document.getElementById("noteBox").value = areaSlot.note || "";

    const items = [];
    ["disease","previous","inside","others"].forEach(group => {
        if (areaSlot[group]) {
            const t = getTool(areaSlot[group]);
    items.push(`<div class="mini-item"><div class="mini-icon">${iconSvg(t.glyph, GROUP_COLORS[group], 22)}</div><div class="mini-text"><div class="key">${group} / area</div><div class="value">${escapeHtml(t.label)}</div></div><button onclick="removeGroupFromArea('${group}')">✕</button></div>`);
        }
    });
    ["disease","previous","inside","others"].forEach(group => {
        if (wholeSlot[group]) {
            const t = getTool(wholeSlot[group]);
    items.push(`<div class="mini-item"><div class="mini-icon">${iconSvg(t.glyph, GROUP_COLORS[group], 22)}</div><div class="mini-text"><div class="key">${group} / whole tooth</div><div class="value">${escapeHtml(t.label)}</div></div><button onclick="removeWholeGroup('${toothId}','${group}')">✕</button></div>`);
        }
    });
    if (areaSlot.note) {
        items.push(`<div class="mini-item"><div class="mini-icon">${iconSvg("note", "#6b7280", 22)}</div><div class="mini-text"><div class="key">note / area</div><div class="value">${escapeHtml(areaSlot.note)}</div></div><button onclick="clearAreaNote()">✕</button></div>`);
    }

    detailList.innerHTML = items.length
    ? items.join("")
    : `<div style="color:#6b7280;font-size:13px;">Area is empty.</div>`;

    document.getElementById("jsonOut").textContent = JSON.stringify(chartData, null, 2);
}

    function renderChart() {
        document.getElementById("chartHost").innerHTML = `
        <div class="chart-grid">
          <div></div>
          ${POS_LABELS.map(n => `<div class="number-cell">${n}</div>`).join("")}

          <div class="side-label">Buccal</div>
          ${UPPER_IDS.map(id => `<div class="tooth-cell">${frontToothSvg(id, "upper")}</div>`).join("")}

          <div class="side-label">Lingual - Palatal</div>
          ${UPPER_IDS.map(id => `<div class="tooth-cell">${occlusalToothSvg(id, "upper")}</div>`).join("")}

          <div></div>
          <div class="mid-rl"><span>R</span><span>L</span></div>

          <div class="side-label">Lingual - Palatal</div>
          ${LOWER_IDS.map(id => `<div class="tooth-cell">${occlusalToothSvg(id, "lower")}</div>`).join("")}

          <div class="side-label">Buccal</div>
          ${LOWER_IDS.map(id => `<div class="tooth-cell">${frontToothSvg(id, "lower")}</div>`).join("")}

          <div></div>
          ${POS_LABELS.map(n => `<div class="number-cell">${n}</div>`).join("")}
        </div>`;
}

    function renderAll() {
        renderToolbar();
    renderChart();
    renderSideInfo();
}

    // ════════════════════════════════════════════════════════════════
    //  DATABASE API
    // ════════════════════════════════════════════════════════════════
    function setDbStatus(msg) {
        document.getElementById("dbStatus").textContent = msg;
}

    async function saveChartToDb() {
    if (!PATIENT_ID) {showToast("❌ Patient ID is not set!", "error"); return; }
    setDbStatus("⏳ Saving...");
    const payload = {
        patientId:     PATIENT_ID,
    chartDataJson: JSON.stringify(chartData),
    sessionNote:   document.getElementById("sessionNoteGlobal").value.trim()
    };
    try {
        const res    = await fetch(`${API_BASE}/save`, {
        method: "POST",
    headers: {"Content-Type": "application/json" },
    body:   JSON.stringify(payload)
        });
    const result = await res.json();
    if (res.ok) {
        setDbStatus("✅ Saved at " + new Date().toLocaleTimeString());
    showToast("✅ Saved successfully!", "success");
        } else {
        setDbStatus("❌ Save failed");
    showToast("❌ " + (result.message || "Save failed"), "error");
        }
    } catch (err) {
        setDbStatus("❌ Connection error");
    showToast("❌ Server connection error", "error");
    console.error(err);
    }
}

    async function loadChartFromDb() {
    if (!PATIENT_ID) return;
    setDbStatus("⏳ Loading...");
    try {
        const res = await fetch(`${API_BASE}/${PATIENT_ID}`);
    if (res.status === 404) {
        setDbStatus("ℹ️ No saved chart found");
    showToast("ℹ️ No saved chart found for this patient", "info");
    return;
        }
    if (!res.ok) {
        setDbStatus("❌ Load failed");
    showToast("❌ Failed to load data", "error");
    return;
        }
    const result = await res.json();
    const loaded = JSON.parse(result.chartDataJson);
        Object.keys(chartData).forEach(k => {
            if (loaded[k]) chartData[k] = loaded[k];
        });
    if (result.sessionNote) {
        document.getElementById("sessionNoteGlobal").value = result.sessionNote;
        }
    renderAll();
    setDbStatus("✅ Loaded at " + new Date().toLocaleTimeString());
    showToast("✅ Data loaded successfully!", "success");
    } catch (err) {
        setDbStatus("❌ Connection error");
    showToast("❌ Server connection error", "error");
    console.error(err);
    }
}

    async function saveNewSession() {
    if (!PATIENT_ID) {showToast("❌ Patient ID is not set!", "error"); return; }
    const note = document.getElementById("sessionNoteGlobal").value.trim()
    || prompt("New session note (optional):") || "";
    const payload = {
        patientId:     PATIENT_ID,
    chartDataJson: JSON.stringify(chartData),
    sessionNote:   note
    };
    try {
        const res    = await fetch(`${API_BASE}/save-session`, {
        method: "POST",
    headers: {"Content-Type": "application/json" },
    body:   JSON.stringify(payload)
        });
    const result = await res.json();
    if (res.ok) {
        showToast(`✅ New session saved (ID: ${result.id})`, "success");
    setDbStatus("✅ New session #" + result.id);
        } else {
        showToast("❌ " + (result.message || "Save failed"), "error");
        }
    } catch (err) {
        showToast("❌ Server connection error", "error");
    console.error(err);
    }
}

    async function openHistory() {
        document.getElementById("historyModal").classList.add("open");
    document.getElementById("historyList").innerHTML = "<p style='color:var(--muted)'>Loading...</p>";
    try {
        const res    = await fetch(`${API_BASE}/history/${PATIENT_ID}`);
    const charts = await res.json();
    if (!charts.length) {
        document.getElementById("historyList").innerHTML = "<p style='color:var(--muted)'>No history found.</p>";
    return;
        }
        document.getElementById("historyList").innerHTML = charts.map(c => `
    <div class="history-item">
        <div>
            <div class="hi-meta">${new Date(c.updatedAt).toLocaleString("en-GB")} &bull; ID: ${c.id}</div>
            <div class="hi-note">${escapeHtml(c.sessionNote || "—")}</div>
        </div>
        <button class="btn btn-primary" onclick="loadSessionById(${c.id})">📥 Load</button>
    </div>
    `).join("");
    } catch (err) {
        document.getElementById("historyList").innerHTML = "<p style='color:red'>Failed to load history.</p>";
    }
}

    function closeHistory() {
        document.getElementById("historyModal").classList.remove("open");
}

    async function loadSessionById(id) {
    try {
        const res = await fetch(`${API_BASE}/single/${id}`);
    if (!res.ok) {showToast("❌ Failed to load session", "error"); return; }
    const result = await res.json();
    const loaded = JSON.parse(result.chartDataJson);
        Object.keys(chartData).forEach(k => {
            if (loaded[k]) chartData[k] = loaded[k];
        });
    if (result.sessionNote) document.getElementById("sessionNoteGlobal").value = result.sessionNote;
    renderAll();
    closeHistory();
    showToast(`✅ Session #${id} loaded`, "success");
    setDbStatus("✅ Session #" + id);
    } catch (err) {
        showToast("❌ Connection error", "error");
    }
}

    // ════════════════════════════════════════════════════════════════
    //  TOAST
    // ════════════════════════════════════════════════════════════════
    function showToast(msg, type = "info") {
    const colors = {success:"#16a34a", error:"#dc2626", info:"#2563eb" };
    const el = document.createElement("div");
    el.className = "toast";
    el.style.background = colors[type] || "#374151";
    el.textContent = msg;
    document.getElementById("toastContainer").appendChild(el);
    setTimeout(() => {el.style.opacity = "0"; }, 2800);
    setTimeout(() => el.remove(), 3200);
}

    // ════════════════════════════════════════════════════════════════
    //  INIT
    // ════════════════════════════════════════════════════════════════
    renderAll();

// Auto-load from DB on page open
if (PATIENT_ID > 0) {
        loadChartFromDb();
}
