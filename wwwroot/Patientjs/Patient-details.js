/*
 * ===========================================================
 * AUTOMATIC MEDICAL RED FLAGS
 * ===========================================================
 */

const AUTOMATIC_RED_FLAG_CONDITIONS = [
    "Bleeding Disorders",
    "Infectious Diseases",
    "Cancer"
];

const RED_FLAG_MEDICATION_CATALOG = [
    {
        generic: "Warfarin",
        brands: ["Marevan", "Coumadin"],
        flagType: "Anticoagulant"
    },
    {
        generic: "Rivaroxaban",
        brands: ["Xarelto"],
        flagType: "Anticoagulant"
    },
    {
        generic: "Apixaban",
        brands: ["Eliquis"],
        flagType: "Anticoagulant"
    },
    {
        generic: "Dabigatran",
        brands: ["Pradaxa"],
        flagType: "Anticoagulant"
    },
    {
        generic: "Edoxaban",
        brands: ["Lixiana"],
        flagType: "Anticoagulant"
    },
    {
        generic: "Enoxaparin",
        brands: ["Clexane"],
        flagType: "Anticoagulant"
    },
    {
        generic: "Aspirin",
        brands: ["Aspocid", "Cardioprin"],
        flagType: "Anticoagulant"
    },
    {
        generic: "Clopidogrel",
        brands: ["Plavix", "Clopivas"],
        flagType: "Anticoagulant"
    },

    {
        generic: "Alendronate",
        brands: ["Fosamax", "Osteomax"],
        flagType: "Bisphosphonates"
    },
    {
        generic: "Risedronate",
        brands: ["Actonel"],
        flagType: "Bisphosphonates"
    },
    {
        generic: "Ibandronate",
        brands: ["Bonviva"],
        flagType: "Bisphosphonates"
    },
    {
        generic: "Zoledronic acid",
        brands: ["Aclasta", "Zometa"],
        flagType: "Bisphosphonates"
    },
    {
        generic: "Pamidronate",
        brands: ["Aredia"],
        flagType: "Bisphosphonates"
    },
    {
        generic: "Denosumab",
        brands: ["Prolia", "Xgeva"],
        flagType: "Bisphosphonates"
    },

    {
        generic: "Prednisolone",
        brands: ["Deltacortril", "Prednisolone Kabi"],
        flagType: "Steroids"
    },
    {
        generic: "Methylprednisolone",
        brands: ["Medrol", "Solu-Medrol"],
        flagType: "Steroids"
    },
    {
        generic: "Dexamethasone",
        brands: ["Dexamethasone"],
        flagType: "Steroids"
    },
    {
        generic: "Betamethasone",
        brands: ["Betnesol", "Celestone"],
        flagType: "Steroids"
    },
    {
        generic: "Hydrocortisone",
        brands: ["Solu-Cortef", "Cortef"],
        flagType: "Steroids"
    },

    {
        generic: "Cyclosporine",
        brands: ["Sandimmune", "Neoral"],
        flagType: "Immunosuppressants"
    },
    {
        generic: "Tacrolimus",
        brands: ["Prograf", "Advagraf"],
        flagType: "Immunosuppressants"
    },
    {
        generic: "Methotrexate",
        brands: ["Methotrexate"],
        flagType: "Immunosuppressants"
    },
    {
        generic: "Azathioprine",
        brands: ["Imuran"],
        flagType: "Immunosuppressants"
    },
    {
        generic: "Mycophenolate mofetil",
        brands: ["CellCept", "Myfortic"],
        flagType: "Immunosuppressants"
    },
    {
        generic: "Adalimumab",
        brands: ["Humira"],
        flagType: "Immunosuppressants"
    },
    {
        generic: "Etanercept",
        brands: ["Enbrel"],
        flagType: "Immunosuppressants"
    },
    {
        generic: "Rituximab",
        brands: ["Mabthera"],
        flagType: "Immunosuppressants"
    },
    {
        generic: "Sirolimus",
        brands: ["Rapamune"],
        flagType: "Immunosuppressants"
    }
];

function normalizeMedicalLookup(value) {
    return (value || "")
        .trim()
        .replace(/\s+/g, " ")
        .toLowerCase();
}

function escapeMedicalHtml(value) {
    const element = document.createElement("div");
    element.textContent = value || "";
    return element.innerHTML;
}

function isAutomaticConditionRedFlag(conditionName) {
    const normalized =
        normalizeMedicalLookup(conditionName);

    return AUTOMATIC_RED_FLAG_CONDITIONS
        .some(function (condition) {
            return normalizeMedicalLookup(condition)
                === normalized;
        });
}

function findMedicationCatalogItem(drugName) {
    const normalized =
        normalizeMedicalLookup(drugName);

    if (!normalized) {
        return null;
    }

    return RED_FLAG_MEDICATION_CATALOG.find(
        function (item) {
            if (
                normalizeMedicalLookup(item.generic)
                === normalized
            ) {
                return true;
            }

            return item.brands.some(
                function (brand) {
                    return (
                        normalizeMedicalLookup(brand)
                        === normalized
                    );
                }
            );
        }
    ) || null;
}

function searchMedicationCatalog(query) {
    const normalizedQuery =
        normalizeMedicalLookup(query);

    if (!normalizedQuery) {
        return [];
    }

    return RED_FLAG_MEDICATION_CATALOG
        .map(function (item) {
            const allNames =
                [item.generic].concat(item.brands);

            const startsWithMatch =
                allNames.some(
                    function (name) {
                        return normalizeMedicalLookup(name)
                            .startsWith(normalizedQuery);
                    }
                );

            const containsMatch =
                allNames.some(
                    function (name) {
                        return normalizeMedicalLookup(name)
                            .includes(normalizedQuery);
                    }
                );

            return {
                item: item,
                score:
                    startsWithMatch
                        ? 0
                        : containsMatch
                            ? 1
                            : 99
            };
        })
        .filter(function (entry) {
            return entry.score < 99;
        })
        .sort(function (left, right) {
            if (left.score !== right.score) {
                return left.score - right.score;
            }

            return left.item.generic
                .localeCompare(right.item.generic);
        })
        .slice(0, 8)
        .map(function (entry) {
            return entry.item;
        });
}

function getMedicationFlagInput(drugInput) {
    const targetId =
        drugInput.dataset.flagTarget;

    if (targetId) {
        return document.getElementById(targetId);
    }

    return drugInput
        .closest(".medication-row-modal")
        ?.querySelector(
            'input[type="hidden"][name$=".FlagType"]'
        ) || null;
}

function getMedicationFlagHint(drugInput) {
    const row =
        drugInput.closest(".medication-row-modal");

    if (row) {
        return row.querySelector(
            ".automatic-red-flag-hint"
        );
    }

    return document.getElementById(
        "editMedicationAutomaticFlagHint"
    );
}

function synchronizeMedicationFlag(drugInput) {
    if (!drugInput) {
        return null;
    }

    const medication =
        findMedicationCatalogItem(
            drugInput.value
        );

    const flagInput =
        getMedicationFlagInput(
            drugInput
        );

    const flagHint =
        getMedicationFlagHint(
            drugInput
        );

    if (flagInput) {
        flagInput.value =
            medication
                ? medication.flagType
                : "";
    }

    if (flagHint) {
        flagHint.style.display =
            medication
                ? "block"
                : "none";
    }

    return medication;
}

function closeMedicationSuggestions(exceptElement) {
    document.querySelectorAll(
        ".medication-suggestions"
    )
        .forEach(function (suggestions) {
            if (suggestions !== exceptElement) {
                suggestions.style.display = "none";
                suggestions.innerHTML = "";
            }
        });
}

function selectMedicationSuggestion(
    drugInput,
    medication
) {
    drugInput.value =
        medication.generic;

    synchronizeMedicationFlag(
        drugInput
    );

    const suggestions =
        document.getElementById(
            drugInput.dataset.suggestionsId
        );

    if (suggestions) {
        suggestions.style.display = "none";
        suggestions.innerHTML = "";
    }

    drugInput.dispatchEvent(
        new Event(
            "change",
            {
                bubbles: true
            }
        )
    );
}

function renderMedicationSuggestions(drugInput) {
    const suggestions =
        document.getElementById(
            drugInput.dataset.suggestionsId
        );

    if (!suggestions) {
        return;
    }

    const results =
        searchMedicationCatalog(
            drugInput.value
        );

    closeMedicationSuggestions(
        suggestions
    );

    if (results.length === 0) {
        suggestions.style.display = "none";
        suggestions.innerHTML = "";
        return;
    }

    suggestions.innerHTML =
        results.map(
            function (medication, index) {
                const brands =
                    medication.brands.length > 0
                        ? medication.brands.join(", ")
                        : "No listed brand names";

                return `
                    <button type="button"
                            class="medication-suggestion-item"
                            data-medication-index="${index}">
                        <div class="medication-suggestion-generic">
                            ${escapeMedicalHtml(medication.generic)}
                        </div>
                        <div class="medication-suggestion-brands">
                            ${escapeMedicalHtml(brands)}
                        </div>
                    </button>
                `;
            }
        )
            .join("");

    suggestions.style.display =
        "block";

    suggestions
        .querySelectorAll(
            ".medication-suggestion-item"
        )
        .forEach(function (button) {
            button.addEventListener(
                "mousedown",
                function (event) {
                    event.preventDefault();

                    const resultIndex =
                        Number(
                            button.dataset.medicationIndex
                        );

                    const medication =
                        results[resultIndex];

                    if (medication) {
                        selectMedicationSuggestion(
                            drugInput,
                            medication
                        );
                    }
                }
            );
        });
}

function initializeMedicationAutocomplete(root) {
    const searchRoot =
        root || document;

    searchRoot
        .querySelectorAll(
            ".medication-drug-input"
        )
        .forEach(function (drugInput) {
            if (
                drugInput.dataset.autocompleteReady
                === "true"
            ) {
                return;
            }

            drugInput.dataset.autocompleteReady =
                "true";

            drugInput.addEventListener(
                "input",
                function () {
                    synchronizeMedicationFlag(
                        drugInput
                    );

                    renderMedicationSuggestions(
                        drugInput
                    );
                }
            );

            drugInput.addEventListener(
                "focus",
                function () {
                    if (
                        drugInput.value.trim()
                    ) {
                        renderMedicationSuggestions(
                            drugInput
                        );
                    }
                }
            );

            drugInput.addEventListener(
                "blur",
                function () {
                    window.setTimeout(
                        function () {
                            synchronizeMedicationFlag(
                                drugInput
                            );

                            const suggestions =
                                document.getElementById(
                                    drugInput.dataset.suggestionsId
                                );

                            if (suggestions) {
                                suggestions.style.display =
                                    "none";
                            }
                        },
                        150
                    );
                }
            );

            synchronizeMedicationFlag(
                drugInput
            );
        });
}

function synchronizeAllMedicationFlags(root) {
    const searchRoot =
        root || document;

    searchRoot
        .querySelectorAll(
            ".medication-drug-input"
        )
        .forEach(function (drugInput) {
            synchronizeMedicationFlag(
                drugInput
            );
        });
}

function applyConditionAutoRedFlag(conditionInput) {
    if (!conditionInput) {
        return;
    }

    const row =
        conditionInput.closest(
            ".condition-row-modal"
        );

    const checkbox =
        row?.querySelector(
            ".condition-red-flag-input"
        );

    const hint =
        row?.querySelector(
            ".automatic-condition-hint"
        );

    const automatic =
        isAutomaticConditionRedFlag(
            conditionInput.value
        );

    if (checkbox && automatic) {
        checkbox.checked = true;
        checkbox.dataset.automatic = "true";
    }
    else if (checkbox) {
        checkbox.dataset.automatic = "false";
    }

    if (hint) {
        hint.style.display =
            automatic
                ? "block"
                : "none";
    }
}

function enforceAutomaticConditionFlag(checkbox) {
    if (
        checkbox
        &&
        checkbox.dataset.automatic === "true"
    ) {
        checkbox.checked = true;
    }
}

function applyEditConditionAutoFlag() {
    const conditionInput =
        document.getElementById(
            "editConditionName"
        );

    const flagCheckbox =
        document.getElementById(
            "editConditionFlagged"
        );

    const hint =
        document.getElementById(
            "editConditionAutomaticHint"
        );

    if (!conditionInput || !flagCheckbox) {
        return;
    }

    const automatic =
        isAutomaticConditionRedFlag(
            conditionInput.value
        );

    if (automatic) {
        flagCheckbox.checked = true;
        flagCheckbox.dataset.automatic = "true";
    }
    else {
        flagCheckbox.dataset.automatic = "false";
    }

    if (hint) {
        hint.style.display =
            automatic
                ? "block"
                : "none";
    }
}

function enforceEditConditionAutoFlag() {
    const flagCheckbox =
        document.getElementById(
            "editConditionFlagged"
        );

    if (
        flagCheckbox
        &&
        flagCheckbox.dataset.automatic
        === "true"
    ) {
        flagCheckbox.checked = true;
    }
}

document.addEventListener(
    "click",
    function (event) {
        if (
            !event.target.closest(
                ".medication-search-wrapper"
            )
        ) {
            closeMedicationSuggestions();
        }
    }
);


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

        document.getElementById(
            'editConditionID'
        ).value = btn.data('id');

        document.getElementById(
            'editConditionName'
        ).value = btn.data('name');

        document.getElementById(
            'editConditionYear'
        ).value = btn.data('year') || '';

        document.getElementById(
            'editConditionActive'
        ).checked =
            String(btn.data('active')) === 'true';

        document.getElementById(
            'editConditionFlagged'
        ).checked =
            String(btn.data('flagged')) === 'true';

        document.getElementById(
            'editConditionNotes'
        ).value = btn.data('notes') || '';

        applyEditConditionAutoFlag();

        document.getElementById(
            'editConditionModal'
        ).style.display = 'flex';
    });

    $(document).on('click', '.edit-med-btn', function () {
        const btn = $(this);

        document.getElementById(
            'editMedID'
        ).value = btn.data('id');

        document.getElementById(
            'editMedDrugName'
        ).value = btn.data('drug');

        document.getElementById(
            'editMedDose'
        ).value = btn.data('dose');

        document.getElementById(
            'editMedFrequency'
        ).value = btn.data('frequency');

        document.getElementById(
            'editMedDuration'
        ).value = btn.data('duration');

        document.getElementById(
            'editMedFlagType'
        ).value = btn.data('flag') || '';

        initializeMedicationAutocomplete(
            document.getElementById(
                'editMedicationModal'
            )
        );

        synchronizeMedicationFlag(
            document.getElementById(
                'editMedDrugName'
            )
        );

        document.getElementById(
            'editMedicationModal'
        ).style.display = 'flex';
    });

    openConditionModal = function () {
        const list =
            document.getElementById(
                'conditions-modal-list'
            );

        list.innerHTML =
            buildConditionRowHTML(0);

        conditionRowIndex = 1;

        list
            .querySelectorAll(
                '.condition-name-input'
            )
            .forEach(function (input) {
                applyConditionAutoRedFlag(input);
            });

        document.getElementById(
            'conditionModal'
        ).style.display = 'flex';
    };

    openMedicationModal = function () {
        const list =
            document.getElementById(
                'medications-modal-list'
            );

        list.innerHTML =
            buildMedicationRowHTML(0);

        medicationRowIndex = 1;

        initializeMedicationAutocomplete(
            list
        );

        document.getElementById(
            'medicationModal'
        ).style.display = 'flex';
    };

    initializeMedicationAutocomplete(
        document
    );
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

    document.querySelectorAll(
        '#conditions-tbody tr'
    )
        .forEach(function (row) {
            const name =
                row.querySelector(
                    '.condition-name-cell'
                )?.textContent.trim();

            const flagged =
                row.querySelector(
                    '.condition-flag-cell'
                )?.textContent.includes('Yes');

            if (name && flagged) {
                flaggedNames.push(name);
            }
        });

    document.querySelectorAll(
        '#medications-tbody tr'
    )
        .forEach(function (row) {
            const name =
                row.querySelector(
                    '.medication-name-cell'
                )?.textContent.trim();

            const flagged =
                row.querySelector(
                    '.medication-flag-cell'
                )?.textContent.includes('Yes');

            if (name && flagged) {
                flaggedNames.push(name);
            }
        });

    const uniqueNames =
        Array.from(
            new Set(flaggedNames)
        );

    const wrapper =
        document.getElementById(
            'info-bar-red-flags-wrapper'
        );

    const dot =
        document.getElementById(
            'info-bar-red-flags-dot'
        );

    const label =
        document.getElementById(
            'info-bar-red-flags-label'
        );

    const text =
        document.getElementById(
            'info-bar-red-flags'
        );

    if (!wrapper || !text) {
        return;
    }

    const hasFlags =
        uniqueNames.length > 0;

    wrapper.style.background =
        hasFlags
            ? '#FDECEA'
            : '#EAF7EE';

    wrapper.style.borderColor =
        hasFlags
            ? '#D9534F'
            : '#27AE60';

    if (dot) {
        dot.style.background =
            hasFlags
                ? '#A32D2D'
                : '#1E8449';
    }

    if (label) {
        label.style.color =
            hasFlags
                ? '#791F1F'
                : '#1A5C35';
    }

    text.style.color =
        hasFlags
            ? '#501313'
            : '#145A32';

    text.textContent =
        hasFlags
            ? uniqueNames.join(', ')
            : 'No red flags';
}

// ═══════════════════════════════════════════════
//  CONDITION — buildRowHTML
// ═══════════════════════════════════════════════
let conditionRowIndex = 1;

function buildConditionRowHTML(
    i,
    removable = false
) {
    return `
        <div class="condition-row-modal"
             style="${removable
            ? 'border-top:1px dashed #ddd; padding-top:12px; margin-top:12px;'
            : ''}">

            ${removable
            ? `
                    <div style="display:flex; justify-content:flex-end; margin-bottom:6px;">
                        <button type="button"
                                onclick="this.closest('.condition-row-modal').remove()"
                                style="background:none; border:none; color:#c0392b; cursor:pointer; font-size:13px;">
                            ✕ Remove
                        </button>
                    </div>
                  `
            : ''}

            <div style="display:grid; grid-template-columns:1fr 1fr; gap:14px; margin-bottom:12px;">
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; margin-bottom:5px; display:block;">
                        Condition
                    </label>

                    <input type="text"
                           name="Conditions[${i}].ConditionName"
                           class="condition-name-input"
                           oninput="applyConditionAutoRedFlag(this)"
                           style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;"
                           placeholder="e.g. Hypertension" />

                    <div class="automatic-condition-hint">
                        🚩 This condition is an automatic Red Flag.
                    </div>
                </div>

                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; margin-bottom:5px; display:block;">
                        Year of Diagnosis
                    </label>

                    <input type="number"
                           name="Conditions[${i}].YearOfDiagnosis"
                           min="1900"
                           max="2100"
                           maxlength="4"
                           oninput="if(this.value.length > 4) this.value = this.value.slice(0, 4);"
                           style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;"
                           placeholder="e.g. 2020" />

                    <label style="display:flex; align-items:center; gap:6px; font-size:12px; margin-top:8px; cursor:pointer; color:#c0392b; font-weight:600;">
                        <input type="checkbox"
                               name="Conditions[${i}].IsFlagged"
                               value="true"
                               class="condition-red-flag-input"
                               onclick="enforceAutomaticConditionFlag(this)"
                               style="accent-color:#c0392b;" />

                        🚩 Red Flag
                    </label>
                </div>
            </div>

            <div style="display:flex; align-items:center; gap:8px; margin-bottom:12px; font-size:13px;">
                <input type="checkbox"
                       name="Conditions[${i}].IsCurrentlyActive"
                       value="true"
                       checked
                       style="accent-color:#252468;" />

                Currently Active
            </div>

            <div>
                <label style="font-size:12px; font-weight:600; color:#555; margin-bottom:5px; display:block;">
                    Notes
                </label>

                <textarea name="Conditions[${i}].Notes"
                          style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box; resize:vertical; min-height:70px;"
                          placeholder="Any additional notes..."></textarea>
            </div>
        </div>
    `;
}

function addModalRow() {
    const list =
        document.getElementById(
            'conditions-modal-list'
        );

    const container =
        document.createElement(
            'div'
        );

    container.innerHTML =
        buildConditionRowHTML(
            conditionRowIndex++,
            true
        );

    const row =
        container.firstElementChild;

    list.appendChild(row);

    const input =
        row.querySelector(
            '.condition-name-input'
        );

    applyConditionAutoRedFlag(
        input
    );
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

function appendConditionsToTable(
    conditions
) {
    const noMessage =
        document.getElementById(
            'no-conditions-msg'
        );

    let table =
        document.getElementById(
            'conditions-table'
        );

    let tableBody =
        document.getElementById(
            'conditions-tbody'
        );

    if (noMessage) {
        noMessage.style.display = 'none';
    }

    if (table) {
        table.style.display = '';
    }

    if (!tableBody) {
        tableBody =
            document.querySelector(
                '#conditions-display tbody'
            );
    }

    conditions.forEach(
        function (condition) {
            const automaticFlag =
                isAutomaticConditionRedFlag(
                    condition.conditionName
                );

            const isFlagged =
                condition.isFlagged === true
                ||
                automaticFlag;

            const isActive =
                condition.isCurrentlyActive === true;

            const row =
                document.createElement(
                    'tr'
                );

            row.innerHTML = `
                <td class="condition-name-cell"
                    style="padding:6px 10px; font-weight:600; color:#252468;">
                    ${escapeMedicalHtml(condition.conditionName)}
                </td>

                <td style="padding:6px 10px;">
                    ${condition.yearOfDiagnosis ?? '---'}
                </td>

                <td style="padding:6px 10px;">
                    ${escapeMedicalHtml(condition.notes || '---')}
                </td>

                <td style="padding:6px 10px;">
                    ${isActive
                    ? '<span style="color:#27ae60; font-weight:600;">Active</span>'
                    : '<span style="color:#aaa;">Inactive</span>'
                }
                </td>

                <td class="condition-flag-cell"
                    style="padding:6px 10px;">
                    ${isFlagged
                    ? '<span style="color:#c0392b; font-weight:700;">🚩 Yes</span>'
                    : '<span style="color:#aaa; font-size:11px;">No</span>'
                }
                </td>

                <td style="padding:6px 10px; text-align:center; white-space:nowrap;">
                    <button type="button"
                            class="edit-condition-btn"
                            data-id="${condition.conditionID}"
                            data-name="${escapeMedicalHtml(condition.conditionName)}"
                            data-year="${condition.yearOfDiagnosis ?? ''}"
                            data-active="${isActive}"
                            data-flagged="${isFlagged}"
                            data-notes="${escapeMedicalHtml(condition.notes || '')}"
                            style="background:none; border:none; color:#252468; cursor:pointer; margin-right:8px;"
                            title="Edit">
                        <i class="fa-solid fa-pen-to-square"></i>
                    </button>

                    <button type="button"
                            onclick="deleteCondition(this, ${condition.conditionID})"
                            style="background:none; border:none; color:#c0392b; cursor:pointer;"
                            title="Delete">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </td>
            `;

            tableBody.appendChild(
                row
            );
        }
    );

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
    applyEditConditionAutoFlag();

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

function buildMedicationRowHTML(
    i,
    removable = false
) {
    const drugInputId =
        `medicationDrugInput-${i}`;

    const suggestionsId =
        `medicationSuggestions-${i}`;

    const flagInputId =
        `medicationFlagType-${i}`;

    return `
        <div class="medication-row-modal"
             style="${removable
            ? 'border-top:1px dashed #ddd; padding-top:12px; margin-top:12px;'
            : ''}">

            ${removable
            ? `
                    <div style="display:flex; justify-content:flex-end; margin-bottom:6px;">
                        <button type="button"
                                onclick="this.closest('.medication-row-modal').remove()"
                                style="background:none; border:none; color:#c0392b; cursor:pointer; font-size:13px;">
                            ✕ Remove
                        </button>
                    </div>
                  `
            : ''}

            <input type="hidden"
                   id="${flagInputId}"
                   name="Medications[${i}].FlagType" />

            <div style="display:grid; grid-template-columns:1fr 1fr; gap:14px; margin-bottom:12px;">
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">
                        Drug Name
                    </label>

                    <div class="medication-search-wrapper">
                        <input type="text"
                               id="${drugInputId}"
                               name="Medications[${i}].DrugName"
                               class="medication-drug-input"
                               data-suggestions-id="${suggestionsId}"
                               data-flag-target="${flagInputId}"
                               autocomplete="off"
                               style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;"
                               placeholder="Search generic or brand name..." />

                        <div id="${suggestionsId}"
                             class="medication-suggestions">
                        </div>
                    </div>

                    <div class="automatic-red-flag-hint">
                        🚩 Red Flag will be assigned automatically.
                    </div>
                </div>

                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">
                        Dose
                    </label>

                    <input type="text"
                           name="Medications[${i}].Dose"
                           style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;"
                           placeholder="e.g. 100mg" />
                </div>
            </div>

            <div style="display:grid; grid-template-columns:1fr 1fr; gap:14px; margin-bottom:12px;">
                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">
                        Frequency
                    </label>

                    <input type="text"
                           name="Medications[${i}].Frequency"
                           style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;"
                           placeholder="e.g. Once daily" />
                </div>

                <div>
                    <label style="font-size:12px; font-weight:600; color:#555; display:block; margin-bottom:5px;">
                        Duration
                    </label>

                    <input type="text"
                           name="Medications[${i}].Duration"
                           style="width:100%; border:1px solid #ddd; border-radius:5px; padding:8px 12px; font-size:13px; box-sizing:border-box;"
                           placeholder="e.g. 3 months" />
                </div>
            </div>
        </div>
    `;
}

function addMedicationRow() {
    const list =
        document.getElementById(
            'medications-modal-list'
        );

    const container =
        document.createElement(
            'div'
        );

    container.innerHTML =
        buildMedicationRowHTML(
            medicationRowIndex++,
            true
        );

    const row =
        container.firstElementChild;

    list.appendChild(
        row
    );

    initializeMedicationAutocomplete(
        row
    );
}

function saveMedications() {
    const form =
        document.getElementById(
            'medicationForm'
        );

    synchronizeAllMedicationFlags(
        form
    );

    const data =
        new FormData(
            form
        );

    const saveButton =
        document.querySelector(
            '#medicationModal button[onclick="saveMedications()"]'
        );

    if (
        !data.get(
            'Medications[0].DrugName'
        )?.trim()
    ) {
        showModalAlert(
            'medicationModal',
            'Please enter at least one drug name.'
        );

        return;
    }

    saveButton.disabled = true;
    saveButton.textContent =
        'Saving...';

    fetch(
        '/Medications/CreateAjax',
        {
            method: 'POST',
            body: data
        }
    )
        .then(function (response) {
            return response.json();
        })
        .then(function (result) {
            if (result.success) {
                closeMedicationModal();

                appendMedicationsToTable(
                    result.medications
                );

                window.isDirty = true;

                showToast(
                    'Medication saved successfully.',
                    'success'
                );
            }
            else {
                showModalAlert(
                    'medicationModal',
                    result.message
                    ||
                    'Error saving medications.'
                );
            }
        })
        .catch(function () {
            showModalAlert(
                'medicationModal',
                'Server error. Please try again.'
            );
        })
        .finally(function () {
            saveButton.disabled = false;
            saveButton.textContent =
                'Save Medications';
        });
}

function appendMedicationsToTable(
    medications
) {
    const noMessage =
        document.getElementById(
            'no-medications-msg'
        );

    let table =
        document.getElementById(
            'medications-table'
        );

    let tableBody =
        document.getElementById(
            'medications-tbody'
        );

    if (noMessage) {
        noMessage.style.display = 'none';
    }

    if (table) {
        table.style.display = '';
    }

    if (!tableBody) {
        tableBody =
            document.querySelector(
                '#medications-display tbody'
            );
    }

    medications.forEach(
        function (medication) {
            const catalogItem =
                findMedicationCatalogItem(
                    medication.drugName
                );

            const isFlagged =
                Boolean(
                    medication.flagType
                )
                ||
                Boolean(
                    catalogItem
                );

            const row =
                document.createElement(
                    'tr'
                );

            row.style.borderBottom =
                '1px solid #eee';

            row.innerHTML = `
                <td class="medication-name-cell"
                    style="padding:6px 10px;">
                    ${escapeMedicalHtml(medication.drugName)}
                </td>

                <td style="padding:6px 10px;">
                    ${escapeMedicalHtml(medication.dose || '---')}
                </td>

                <td style="padding:6px 10px;">
                    ${escapeMedicalHtml(medication.frequency || '---')}
                </td>

                <td style="padding:6px 10px;">
                    ${escapeMedicalHtml(medication.duration || '---')}
                </td>

                <td class="medication-flag-cell"
                    style="padding:6px 10px;">
                    ${isFlagged
                    ? '<span style="color:#c0392b; font-weight:700;">🚩 Yes</span>'
                    : '<span style="color:#aaa; font-size:11px;">No</span>'
                }
                </td>

                <td style="padding:6px 10px; text-align:center; white-space:nowrap;">
                    <button type="button"
                            class="edit-med-btn"
                            data-id="${medication.medicationID}"
                            data-drug="${escapeMedicalHtml(medication.drugName)}"
                            data-dose="${escapeMedicalHtml(medication.dose || '')}"
                            data-frequency="${escapeMedicalHtml(medication.frequency || '')}"
                            data-duration="${escapeMedicalHtml(medication.duration || '')}"
                            data-flag="${escapeMedicalHtml(
                    medication.flagType
                    ||
                    catalogItem?.flagType
                    ||
                    ''
                )}"
                            style="background:none; border:none; color:#252468; cursor:pointer; margin-right:8px;"
                            title="Edit">
                        <i class="fa-solid fa-pen-to-square"></i>
                    </button>

                    <button type="button"
                            onclick="deleteMedication(this, ${medication.medicationID})"
                            style="background:none; border:none; color:#c0392b; cursor:pointer;"
                            title="Delete">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </td>
            `;

            tableBody.appendChild(
                row
            );
        }
    );

    updateRedFlagsInfoBar();
}

function closeMedicationModal() {
    closeMedicationSuggestions();

    document.getElementById(
        'medicationModal'
    ).style.display = 'none';
}

function closeEditMedicationModal() {
    closeMedicationSuggestions();

    document.getElementById(
        'editMedicationModal'
    ).style.display = 'none';
}

function deleteMedication(
    button,
    medicationId
) {
    if (
        !confirm(
            'Are you sure you want to delete this medication?'
        )
    ) {
        return;
    }

    const row =
        button.closest('tr');

    fetch(
        '/Medications/DeleteAjax',
        {
            method: 'POST',

            headers: {
                'Content-Type':
                    'application/x-www-form-urlencoded'
            },

            body:
                `id=${medicationId}`
                +
                `&__RequestVerificationToken=${encodeURIComponent(getAntiForgeryToken())}`
        }
    )
        .then(function (response) {
            return response.json();
        })
        .then(function (result) {
            if (result.success) {
                row.remove();

                const tableBody =
                    document.getElementById(
                        'medications-tbody'
                    );

                if (
                    tableBody
                    &&
                    tableBody.querySelectorAll(
                        'tr'
                    ).length === 0
                ) {
                    const noMessage =
                        document.getElementById(
                            'no-medications-msg'
                        );

                    const table =
                        document.getElementById(
                            'medications-table'
                        );

                    if (noMessage) {
                        noMessage.style.display = '';
                    }

                    if (table) {
                        table.style.display = 'none';
                    }
                }

                updateRedFlagsInfoBar();

                showToast(
                    'Medication deleted successfully.',
                    'success'
                );
            }
            else {
                showToast(
                    'Error deleting medication.',
                    'error'
                );
            }
        })
        .catch(function () {
            showToast(
                'Server error.',
                'error'
            );
        });
}

function submitEditMedication() {
    const form =
        document.getElementById(
            'editMedicationForm'
        );

    synchronizeAllMedicationFlags(
        form
    );

    const data =
        new FormData(
            form
        );

    const saveButton =
        document.getElementById(
            'editMedSaveBtn'
        );

    saveButton.disabled = true;
    saveButton.textContent =
        'Saving...';

    fetch(
        '/Medications/EditAjax',
        {
            method: 'POST',
            body: data
        }
    )
        .then(function (response) {
            return response.json();
        })
        .then(function (result) {
            if (result.success) {
                closeEditMedicationModal();

                showToast(
                    'Medication updated successfully.',
                    'success'
                );

                window.setTimeout(
                    function () {
                        location.reload();
                    },
                    1200
                );
            }
            else {
                showToast(
                    result.message
                    ||
                    'Error updating medication.',
                    'error'
                );
            }
        })
        .catch(function () {
            showToast(
                'Server error.',
                'error'
            );
        })
        .finally(function () {
            saveButton.disabled = false;
            saveButton.textContent =
                'Save Changes';
        });
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